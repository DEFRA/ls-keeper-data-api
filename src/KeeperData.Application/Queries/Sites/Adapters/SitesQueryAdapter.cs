using KeeperData.Application.Queries.Sites.Builders;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Sites.Adapters;

public class SitesQueryAdapter(ISitesRepository repository)
{
    private readonly ISitesRepository _repository = repository;

    public async Task<(List<SiteDocument> Items, int TotalCount)> GetSitesAsync(
        GetSitesQuery query,
        CancellationToken cancellationToken = default)
    {
        var filterDefinition = SiteFilterBuilder.Build(query);
        var sortDefinition = SiteSortBuilder.Build(query);

        var totalCount = await _repository.CountAsync(filterDefinition, cancellationToken);

        var items = await _repository.FindAsync(
            filter: filterDefinition,
            sort: sortDefinition,
            skip: (query.Page - 1) * query.PageSize,
            take: query.PageSize,
            cancellationToken: cancellationToken);

        return (items, totalCount);
    }
}
