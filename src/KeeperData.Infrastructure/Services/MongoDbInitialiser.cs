using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection;

namespace KeeperData.Infrastructure.Services
{
    public class MongoDbInitialiser(
        IMongoClient mongoClient,
        IOptions<MongoConfig> mongoConfig) : IMongoDbInitialiser
    {
        private readonly IMongoClient _mongoClient = mongoClient;
        private readonly IOptions<MongoConfig> _mongoConfig = mongoConfig;

        public async Task Initialise(Type type)
        {
            var _database = _mongoClient.GetDatabase(_mongoConfig.Value.DatabaseName);
            if (!type.IsAssignableTo(typeof(IContainsIndexes)))
                throw new ArgumentException($"Type {type.Name} must be assignable to {nameof(IContainsIndexes)}");

            var collectionName = type.GetCustomAttribute<CollectionNameAttribute>()?.Name ?? type.Name;
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            var getIndexesMethod = type.GetMethod("GetIndexModels", BindingFlags.Public | BindingFlags.Static);
            if (getIndexesMethod?.Invoke(null, null) is IEnumerable<CreateIndexModel<BsonDocument>> indexModels)
            {
                await collection.Indexes.CreateManyAsync(indexModels);
            }
        }
    }
}