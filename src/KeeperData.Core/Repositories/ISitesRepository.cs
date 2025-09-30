using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Core.Repositories;

public interface ISitesRepository : IGenericRepository<SiteDocument>
{
    Task<int> CountAsync(
        FilterDefinition<SiteDocument> filter,
        CancellationToken cancellationToken = default);
    
    Task<List<SiteDocument>> FindAsync(
        FilterDefinition<SiteDocument> filter,
        SortDefinition<SiteDocument> sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
