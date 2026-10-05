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
                var indexModelList = indexModels.ToList();

                await DropStaleIndexesAsync(collection, indexModelList);
                await collection.Indexes.CreateManyAsync(indexModelList);
            }
        }

        /// <summary>
        /// Drops any index present on the collection that is no longer declared in code (e.g. left
        /// behind by a rename such as the former unique "uidx_email" index). Without this, a renamed
        /// or relaxed index would otherwise still be created, but the superseded one would keep
        /// existing - and enforcing its old constraints - alongside it.
        /// </summary>
        private static async Task DropStaleIndexesAsync(
            IMongoCollection<BsonDocument> collection,
            List<CreateIndexModel<BsonDocument>> desiredIndexModels)
        {
            var desiredNames = desiredIndexModels
                .Select(m => m.Options?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToHashSet(StringComparer.Ordinal);

            using var cursor = await collection.Indexes.ListAsync();
            var existingIndexes = await cursor.ToListAsync();

            foreach (var existingIndex in existingIndexes)
            {
                var name = existingIndex.GetValue("name", BsonNull.Value).AsString;

                if (string.IsNullOrEmpty(name) || name == "_id_" || desiredNames.Contains(name))
                    continue;

                await collection.Indexes.DropOneAsync(name);
            }
        }
    }
}