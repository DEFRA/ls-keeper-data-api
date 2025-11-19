using System.Reflection;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Helpers;

public class MongoVerifier
{
    private readonly IMongoDatabase _database;

    public MongoVerifier(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public async Task<bool> DocumentExistsAsync<TDocument>(
        string collectionName,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<TDocument>(collectionName);
        var count = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task<List<TDocument>> FindDocumentsAsync<TDocument>(
        string collectionName,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default)
    {
        var collection = _database.GetCollection<TDocument>(collectionName);
        return await collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task Insert<T>(IEnumerable<T> entities) where T : IEntity
    {
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>()?.Name ?? typeof(T).Name;
        var collection = _database.GetCollection<T>(collectionName);
        await collection.InsertManyAsync(entities, new InsertManyOptions { BypassDocumentValidation = true });
    }

    public async Task Delete<T>(IEnumerable<T> entities) where T : IEntity
    {
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>()?.Name ?? typeof(T).Name;
        var collection = _database.GetCollection<T>(collectionName);

        var ids = entities.Select(e => e.Id).ToList();
        await collection.DeleteManyAsync(Builders<T>.Filter.In("_id", ids));
    }

    public async Task DeleteAll<T>() where T : IEntity
    {
        var collectionName = typeof(T).GetCustomAttribute<CollectionNameAttribute>()?.Name ?? typeof(T).Name;
        var collection = _database.GetCollection<T>(collectionName);
        await collection.DeleteManyAsync(FilterDefinition<T>.Empty);
    }
}