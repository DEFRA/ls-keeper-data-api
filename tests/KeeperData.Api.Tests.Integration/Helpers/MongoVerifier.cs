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
}