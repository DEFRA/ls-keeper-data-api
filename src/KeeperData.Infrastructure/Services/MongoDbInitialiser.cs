using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection;

namespace KeeperData.Infrastructure.Services
{
    public class MongoDbInitialiser : IMongoDbInitialiser
    {
        private readonly IMongoClient _mongoClient;
        private readonly IOptions<MongoConfig> _mongoConfig;
        private readonly ILogger<MongoDbInitialiser> _logger;

        public MongoDbInitialiser(
            IMongoClient mongoClient,
            IOptions<MongoConfig> mongoConfig,
            ILogger<MongoDbInitialiser> logger)
        {
            _mongoClient = mongoClient;
            _mongoConfig = mongoConfig;
            _logger = logger;
        }

        public async Task Initialise(Type type)
        {
            var _database = _mongoClient.GetDatabase(_mongoConfig.Value.DatabaseName);
            if (!type.IsAssignableTo(typeof(IContainsIndexes)))
                throw new ArgumentException($"Type {type.Name} must be assignable to {nameof(IContainsIndexes)}");

            var collectionName = type.GetCustomAttribute<CollectionNameAttribute>()?.Name ?? type.Name;
            var collection = _database.GetCollection<BsonDocument>(collectionName);

            // Explicitly drop the conflicting index before general cleanup
            try { await collection.Indexes.DropOneAsync("idxv2_customerNumber"); } catch { }

            await DropV1IndexesIfPresentAsync(collection, _logger);

            var getIndexesMethod = type.GetMethod("GetIndexModels", BindingFlags.Public | BindingFlags.Static);
            if (getIndexesMethod?.Invoke(null, null) is IEnumerable<CreateIndexModel<BsonDocument>> indexModels)
            {
                await collection.Indexes.CreateManyAsync(indexModels);
            }
        }

        private static async Task DropV1IndexesIfPresentAsync(IMongoCollection<BsonDocument> collection, ILogger logger)
        {
            using var cursor = await collection.Indexes.ListAsync();
            var indexes = await cursor.ToListAsync();

            foreach (var index in indexes)
            {
                await DropIndexIfItIsV1(collection, index, logger);
            }
        }

        private static async Task DropIndexIfItIsV1<TDocument>(IMongoCollection<TDocument> collection, BsonDocument index, ILogger logger)
        {
            var indexName = index["name"].AsString;
            if (indexName.StartsWith("idx_") || indexName == "idxv2_customerNumber")
            {
                await collection.Indexes.DropOneAsync(indexName);
                logger.LogInformation("Dropped index: {IndexName}", indexName);
            }
        }
    }
}