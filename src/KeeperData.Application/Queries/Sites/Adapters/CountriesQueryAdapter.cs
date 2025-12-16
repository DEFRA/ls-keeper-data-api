using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Sites.Adapters;

public class CountriesQueryAdapter(ICountryRepository repository)
{
    private readonly ICountryRepository _repository = repository;

    public async Task<(List<CountrySummaryDocument> Items, int TotalCount)> GetCountriesAsync(
        GetCountriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync();
        return (items.Select(c => new CountrySummaryDocument { Name = "c.", Code = "abc", IdentifierId = "123", LongName = "abcd" }).ToList(), items.Count());
    }
}