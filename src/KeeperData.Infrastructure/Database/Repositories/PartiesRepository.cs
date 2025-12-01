using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class PartiesRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : GenericRepository<PartyDocument>(
        mongoConfig,
        client,
        unitOfWork), IPartiesRepository
{
    public async Task<int> CountAsync(FilterDefinition<PartyDocument> filter, CancellationToken cancellationToken = default)
    {
        return (int)await _collection.CountDocumentsAsync(filter, new CountOptions { Collation = new Collation(locale: "en", strength: CollationStrength.Primary, caseLevel: false) }, cancellationToken: cancellationToken);
    }

    public async Task<List<PartyDocument>> FindAsync(
        FilterDefinition<PartyDocument> filter,
        SortDefinition<PartyDocument> sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return
            await _collection
            .Find(filter, options: new FindOptions { Collation = new Collation(locale: "en", strength: CollationStrength.Primary, caseLevel: false) })
            .Sort(sort)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);
    }
}