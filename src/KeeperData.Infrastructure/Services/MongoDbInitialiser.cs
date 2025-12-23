using System.Reflection;
using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Services
{
    public class MongoDbInitialiser(IMongoDatabase database) : IMongoDbInitialiser
    {
        private readonly IMongoDatabase _database = database;

        public async Task Initialise(Type type)
        {
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

    public interface IMongoDbInitialiser
    {
        public Task Initialise(Type type);
    }
}