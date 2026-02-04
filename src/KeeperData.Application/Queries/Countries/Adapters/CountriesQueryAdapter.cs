using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Countries.Adapters;

public class CountriesQueryAdapter(ICountryRepository repository)
{
    private readonly ICountryRepository _repository = repository;

    public async Task<(List<CountryDTO> Items, int TotalCount)> GetCountriesAsync(
        GetCountriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(cancellationToken);

        var results = items
            .Where(c => !query.LastUpdatedDate.HasValue || c.LastModifiedDate >= query.LastUpdatedDate.Value)
            .Where(c => !query.EuTradeMember.HasValue || c.EuTradeMember == query.EuTradeMember)
            .Where(c => !query.DevolvedAuthority.HasValue || c.DevolvedAuthority == query.DevolvedAuthority)
            .Where(c => string.IsNullOrEmpty(query.Name) || c.Name.Contains(query.Name, StringComparison.InvariantCultureIgnoreCase))
            .Where(c => !(query.Code is { Count: > 0 }) || query.Code.Contains(c.Code))
            .ToList(); // ToList to avoid multiple enumeration and get the count easily


        var sortedResults = query.Order?.ToLowerInvariant() switch
        {
            "name" => query.Sort?.ToLowerInvariant() == "desc" ? results.OrderByDescending(c => c.Name) : results.OrderBy(c => c.Name),
            _ => query.Sort?.ToLowerInvariant() == "desc" ? results.OrderByDescending(c => c.Code) : results.OrderBy(c => c.Code)
        };


        var pagedItems = sortedResults
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CountryDTO
            {
                Name = c.Name,
                Code = c.Code,
                IdentifierId = c.IdentifierId,
                LongName = c.LongName,
                DevolvedAuthorityFlag = c.DevolvedAuthority,
                EuTradeMemberFlag = c.EuTradeMember,
                LastUpdatedDate = c.LastModifiedDate
            })
            .ToList();

        return (pagedItems, results.Count);
    }
}