using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class SitesRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : GenericRepository<SiteDocument>(
        mongoConfig,
        client,
        unitOfWork), ISitesRepository
{
    public async Task<int> CountAsync(FilterDefinition<SiteDocument> filter, CancellationToken cancellationToken = default)
    {
        return (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<List<SiteDocument>> FindAsync(
        FilterDefinition<SiteDocument> filter,
        SortDefinition<SiteDocument> sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _collection.Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(take)
            .ToListAsync(cancellationToken);
    }
}