using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class ScanStateRepository : IScanStateRepository
{
    private readonly IMongoCollection<ScanStateDocument> _collection;

    public ScanStateRepository(IOptions<MongoConfig> mongoConfig, IMongoClient client)
    {
        var database = client.GetDatabase(mongoConfig.Value.DatabaseName);
        _collection = database.GetCollection<ScanStateDocument>("scanState");
    }

    public async Task<ScanStateDocument?> GetByIdAsync(string scanSourceId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScanStateDocument>.Filter.Eq(x => x.Id, scanSourceId);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(ScanStateDocument scanState, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ScanStateDocument>.Filter.Eq(x => x.Id, scanState.Id);
        await _collection.ReplaceOneAsync(
            filter,
            scanState,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IEnumerable<ScanStateDocument>> GetAllAsync(int skip, int limit, CancellationToken cancellationToken = default)
    {
        var sortDefinition = Builders<ScanStateDocument>.Sort.Descending(x => x.LastSuccessfulScanCompletedAt);
        var cursor = await _collection.Find(Builders<ScanStateDocument>.Filter.Empty)
            .Sort(sortDefinition)
            .Skip(skip)
            .Limit(limit)
            .ToCursorAsync(cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(Builders<ScanStateDocument>.Filter.Empty, cancellationToken: cancellationToken);
        return (int)count;
    }
}