using KeeperData.Infrastructure.Database.Factories;
using KeeperData.Infrastructure.Database.Factories.Implementations;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Behaviors;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using KeeperData.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;
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
        services.AddSingleton(sp => sp.GetRequiredService<IMongoDbClientFactory>().CreateClient());

        services.AddSingleton<IMongoDbInitialiser, MongoDbInitialiser>();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        services.AddScoped<ICphRepository, CphRepository>();
        services.AddScoped<ICphAssociationsRepository, CphAssociationsRepository>();
        services.AddScoped<IHoldingDetailRepository, HoldingDetailRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<ISpeciesRepository, SpeciesRepository>();
        services.AddScoped<IFacilityBusinessActivityMapRepository, FacilityBusinessActivityMapRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ISiteTypeRepository, SiteTypeRepository>();
        services.AddScoped<ISiteActivityTypeRepository, SiteActivityTypeRepository>();
        services.AddScoped<IProductionUsageRepository, ProductionUsageRepository>();
        services.AddScoped<ISiteIdentifierTypeRepository, SiteIdentifierTypeRepository>();
        services.AddScoped<ISiteTypeMapRepository, SiteTypeMapRepository>();
        services.AddScoped<ISitesRepository, SitesRepository>();
        services.AddScoped<IPartiesRepository, PartiesRepository>();
        services.AddScoped<IUserAccountsRepository, UserAccountsRepository>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

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

                    RegisterAllDocumentsFromAssembly(typeof(INestedEntity).Assembly);

                    // Deployed environments authenticate to DocumentDB with authMechanism=MONGODB-AWS, which
                    // driver 3.x only supports once this opt-in runs. The registry throws on a second call.
                    MongoClientSettings.Extensions.AddAWSAuthentication();

                    s_mongoSerializersRegistered = true;
                }
            }
        }
    }

    private static void RegisterAllDocumentsFromAssembly(Assembly assembly)
    {
        var documentTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(INestedEntity).IsAssignableFrom(t))
            .Where(t => !BsonClassMap.IsClassMapRegistered(t));

        foreach (var type in documentTypes)
        {
            BsonClassMap.LookupClassMap(type);
        }
    }
}