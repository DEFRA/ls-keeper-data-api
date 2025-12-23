using System.Reflection;
using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Config;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Services
{
    public class MongoDbPreproductionService : IMongoDbPreproductionService
    {
        private readonly IMongoDatabase _db;
        private readonly IMongoDbInitialiser _initialiser;
        private readonly MongoDbPreproductionServiceConfig _config;

        public MongoDbPreproductionService(IMongoClient mongoClient, IOptions<MongoConfig> mongoConfig, IMongoDbInitialiser initialiser, IOptions<MongoDbPreproductionServiceConfig> config)
        {
            _initialiser = initialiser;
            _config = config.Value;
            _db = mongoClient.GetDatabase(mongoConfig.Value.DatabaseName);
        }

        public async Task<string> WipeCollection(string collectionName)
        {
            var indexableTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(x => x.GetTypes())
                .Where(t => typeof(IEntity).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            var targetType = indexableTypes.SingleOrDefault(t => t.GetCustomAttribute<CollectionNameAttribute>()?.Name == collectionName);

            if (targetType == null)
                throw new ArgumentException($"Collection name '{collectionName}' does not match an IEntity mapping");

            if (!_config.PermittedTables.Contains(collectionName))
                throw new ArgumentException($"Wipe is not permitted for collection '{collectionName}' ");

            await _db.DropCollectionAsync(collectionName, CancellationToken.None);
            await _initialiser.Initialise(targetType);

            return $"wiped {collectionName}";
        }
    }
}