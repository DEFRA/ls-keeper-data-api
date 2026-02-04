using KeeperData.Application.Queries.Countries.Adapters;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;

namespace KeeperData.Application.Queries.Countries;

public class GetCountriesQueryHandler(CountriesQueryAdapter adapter)
    : PagedQueryHandler<GetCountriesQuery, CountryDTO>
{
    private readonly CountriesQueryAdapter _adapter = adapter;

    protected override async Task<(List<CountryDTO> Items, int TotalCount)> FetchAsync(GetCountriesQuery request, CancellationToken cancellationToken)
    {
        return await _adapter.GetCountriesAsync(request, cancellationToken);
    }
}