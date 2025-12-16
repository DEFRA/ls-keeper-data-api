using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Core.Documents;

namespace KeeperData.Application.Queries.Sites;

public class GetCountriesQueryHandler(CountriesQueryAdapter adapter)
    : PagedQueryHandler<GetCountriesQuery, CountrySummaryDocument>
{
    private readonly CountriesQueryAdapter _adapter = adapter;

    protected override async Task<(List<CountrySummaryDocument> Items, int TotalCount)> FetchAsync(GetCountriesQuery query, CancellationToken cancellationToken)
    {
        return await _adapter.GetCountriesAsync(query, cancellationToken);
    }
}