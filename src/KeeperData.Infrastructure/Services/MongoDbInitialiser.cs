using System.Reflection;
using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Services
{
    public class MongoDbInitialiser : IMongoDbInitialiser
    {
        private IMongoClient _mongoClient;
        private IOptions<MongoConfig> _mongoConfig;

        public MongoDbInitialiser(IMongoClient mongoClient, IOptions<MongoConfig> mongoConfig)
        {
            _mongoClient = mongoClient;
            _mongoConfig = mongoConfig;
        }

        public async Task Initialise(Type type)
        {
            var _database = _mongoClient.GetDatabase(_mongoConfig.Value.DatabaseName);
            if (!type.IsAssignableTo(typeof(IContainsIndexes)))
                throw new ArgumentException($"Type {type.Name} must be assignable to {nameof(IContainsIndexes)}");

            var collectionName = type.GetCustomAttribute<CollectionNameAttribute>()?.Name ?? type.Name;
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            await DropV1IndexesIfPresentAsync(collection);

            var getIndexesMethod = type.GetMethod("GetIndexModels", BindingFlags.Public | BindingFlags.Static);
            if (getIndexesMethod?.Invoke(null, null) is IEnumerable<CreateIndexModel<BsonDocument>> indexModels)
            {
                await collection.Indexes.CreateManyAsync(indexModels);
            }
        }

        private static async Task DropV1IndexesIfPresentAsync<TDocument>(IMongoCollection<TDocument> collection)
        {
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();

            foreach (var index in indexes)
            {
                var indexName = index["name"].AsString;
                if (indexName.StartsWith("idx_"))
                {
                    await collection.Indexes.DropOneAsync(indexName);
                    Console.WriteLine($"Dropped index: {indexName}");
                }
            }
        }
    }
}