using KeeperData.Application.Queries.Countries;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Sites;

public class GetCountriesQueryHandler(CountriesQueryAdapter adapter)
    : PagedQueryHandler<GetCountriesQuery, CountryDTO>
{
    private readonly CountriesQueryAdapter _adapter = adapter;

    protected override async Task<(List<CountryDTO> Items, int TotalCount)> FetchAsync(GetCountriesQuery query, CancellationToken cancellationToken)
    {
        return await _adapter.GetCountriesAsync(query, cancellationToken);
    }
}