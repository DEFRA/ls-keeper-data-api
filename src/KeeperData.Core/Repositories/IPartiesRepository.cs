using KeeperData.Core.Documents;
using MongoDB.Driver;

namespace KeeperData.Core.Repositories;

public interface IPartiesRepository : IGenericRepository<PartyDocument>
{
    Task<int> CountAsync(
        FilterDefinition<PartyDocument> filter,
        CancellationToken cancellationToken = default);

    Task<List<PartyDocument>> FindAsync(
        FilterDefinition<PartyDocument> filter,
        SortDefinition<PartyDocument> sort,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<PartyDocument?> FindPartyByCustomerNumber(string customerNumber, CancellationToken cancellationToken = default);
}