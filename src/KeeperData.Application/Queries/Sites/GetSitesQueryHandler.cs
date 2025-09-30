using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Sites;

public class GetSitesQueryHandler(SitesQueryAdapter adapter) 
    : PagedQueryHandler<GetSitesQuery, SiteDocument>
{
    private readonly SitesQueryAdapter _adapter = adapter;

    protected override async Task<(List<SiteDocument> Items, int TotalCount)> FetchAsync(GetSitesQuery query, CancellationToken cancellationToken)
    {
        return await _adapter.GetSitesAsync(query, cancellationToken);
    }
}
