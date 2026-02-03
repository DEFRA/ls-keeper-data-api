using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Sites;

public class GetSitesQueryHandler(SitesQueryAdapter adapter)
    : PagedQueryHandler<GetSitesQuery, SiteDocument>
{
    private readonly SitesQueryAdapter _adapter = adapter;

    protected override async Task<(List<SiteDocument> Items, int TotalCount)> FetchAsync(GetSitesQuery request, CancellationToken cancellationToken)
    {
        var (siteDocuments, totalCount) = await _adapter.GetSitesAsync(request, cancellationToken);

        return (siteDocuments, totalCount);
    }
}