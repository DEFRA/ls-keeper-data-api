using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.BuildingBlocks.Aggregates;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Behaviors;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Factories;
using KeeperData.Infrastructure.Database.Factories.Implementations;
using KeeperData.Infrastructure.Database.Repositories;
using KeeperData.Infrastructure.Database.Transactions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace KeeperData.Infrastructure.Database.Setup;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    private static bool s_mongoSerializersRegistered;

    public static void AddDatabaseDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterMongoDbGlobals();

        var mongoConfig = configuration.GetSection("Mongo").Get<MongoConfig>()!;
        services.Configure<MongoConfig>(configuration.GetSection("Mongo"));

        services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();
        services.AddScoped<IMongoSessionFactory, MongoSessionFactory>();

        services.AddScoped(sp => sp.GetRequiredService<IMongoSessionFactory>().GetSession());
        services.AddSingleton(sp => sp.GetRequiredService<IMongoDbClientFactory>().CreateClient());

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ISitesRepository, SitesRepository>();

        services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
        services.AddScoped(sp => (ITransactionManager)sp.GetRequiredService<IUnitOfWork>());
        services.AddScoped<IAggregateTracker, AggregateTracker>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkTransactionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainEventDispatchingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AggregateRootChangedBehavior<,>));

        if (mongoConfig.HealthcheckEnabled)
        {
            services.AddHealthChecks()
                .AddCheck<MongoDbHealthCheck>("mongodb", tags: ["db", "mongo"]);
        }

        var provider = services.BuildServiceProvider();
        EnsureMongoIndexesAsync(provider).GetAwaiter().GetResult();
    }

    private static void RegisterMongoDbGlobals()
    {
        if (!s_mongoSerializersRegistered)
        {
            lock (typeof(ServiceCollectionExtensions))
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

    private static async Task EnsureMongoIndexesAsync(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IMongoClient>();
        var config = serviceProvider.GetRequiredService<IOptions<MongoConfig>>().Value;
        var database = client.GetDatabase(config.DatabaseName);

        var indexableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(IContainsIndexes).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in indexableTypes)
        {
            var collectionName = type.GetCustomAttribute<CollectionNameAttribute>()?.Name ?? type.Name;
            var collection = database.GetCollection<BsonDocument>(collectionName);

            var getIndexesMethod = type.GetMethod("GetIndexModels", BindingFlags.Public | BindingFlags.Static);
            if (getIndexesMethod?.Invoke(null, null) is IEnumerable<CreateIndexModel<BsonDocument>> indexModels)
            {
                await collection.Indexes.CreateManyAsync(indexModels);
            }
        }
    }

    public static void RegisterAllDocumentsFromAssembly(Assembly assembly)
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