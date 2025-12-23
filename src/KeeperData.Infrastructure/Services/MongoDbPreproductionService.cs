using System.Reflection;
using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Config;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Services
{
    public class MongoDbPreproductionService(IMongoDatabase db, IMongoDbInitialiser initialiser, MongoDbPreproductionServiceConfig config) : IMongoDbPreproductionService
    {
        private IMongoDatabase _db = db;
        private readonly IMongoDbInitialiser _initialiser = initialiser;
        private readonly MongoDbPreproductionServiceConfig _config = config;

        public async Task WipeCollection(string collectionName)
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
        }
    }
}