using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Locking;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Behaviors;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Factories;
using KeeperData.Infrastructure.Database.Factories.Implementations;
using KeeperData.Infrastructure.Database.Repositories;
using KeeperData.Infrastructure.Database.Transactions;
using KeeperData.Infrastructure.Locking;
using KeeperData.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace KeeperData.Infrastructure.Database.Setup;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    private static bool s_mongoSerializersRegistered;
    private static readonly object s_mongoSerializersLock = new();

    public static void AddDatabaseDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterMongoDbGlobals();

        var mongoConfig = configuration.GetSection("Mongo").Get<MongoConfig>()!;
        services.Configure<MongoConfig>(configuration.GetSection("Mongo"));

        services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();
        services.AddScoped<IMongoSessionFactory, MongoSessionFactory>();

        services.AddScoped(sp => sp.GetRequiredService<IMongoSessionFactory>().GetSession());
        services.AddSingleton(sp => sp.GetRequiredService<IMongoDbClientFactory>().CreateClient());

        services.AddSingleton<IMongoDbInitialiser, MongoDbInitialiser>();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<ISpeciesRepository, SpeciesRepository>();
        services.AddScoped<IFacilityBusinessActivityMapRepository, FacilityBusinessActivityMapRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPremisesTypeRepository, PremisesTypeRepository>();
        services.AddScoped<IPremisesActivityTypeRepository, PremisesActivityTypeRepository>();
        services.AddScoped<IProductionUsageRepository, ProductionUsageRepository>();
        services.AddScoped<ISiteIdentifierTypeRepository, SiteIdentifierTypeRepository>();
        services.AddScoped<ISitesRepository, SitesRepository>();
        services.AddScoped<IPartiesRepository, PartiesRepository>();
        services.AddScoped<IGoldSitePartyRoleRelationshipRepository, GoldSitePartyRoleRelationshipRepository>();

        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        services.AddScoped(sp => (ITransactionManager)sp.GetRequiredService<IUnitOfWork>());
        services.AddScoped<IAggregateTracker, AggregateTracker>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkTransactionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainEventDispatchingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AggregateRootChangedBehavior<,>));

        services.AddSingleton<IDistributedLock, MongoDistributedLock>();

        if (mongoConfig.HealthcheckEnabled)
        {
            services.AddHealthChecks()
                .AddCheck<MongoDbHealthCheck>("mongodb", tags: ["db", "mongo"]);
        }

        services.AddHostedService<MongoIndexInitializer>();
    }

    private static void RegisterMongoDbGlobals()
    {
        if (!s_mongoSerializersRegistered)
        {
            lock (s_mongoSerializersLock)
            {
                if (!s_mongoSerializersRegistered)
                {
                    BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
                    ConventionRegistry.Register("CamelCase", new ConventionPack { new CamelCaseElementNameConvention() }, _ => true);
                    s_mongoSerializersRegistered = true;

                    RegisterAllDocumentsFromAssembly(typeof(INestedEntity).Assembly);
                }
            }
        }
    }

    private static void RegisterAllDocumentsFromAssembly(Assembly assembly)
    {
        var documentTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(INestedEntity).IsAssignableFrom(t));

        foreach (var type in documentTypes)
        {
            if (!BsonClassMap.IsClassMapRegistered(type))
            {
                BsonClassMap.LookupClassMap(type);
            }
        }
    }
}