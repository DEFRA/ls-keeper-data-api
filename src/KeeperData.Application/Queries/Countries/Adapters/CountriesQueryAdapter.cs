using System.Collections.Immutable;
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

        var codes = query!.Code?.Split(',');
        var results = items
            .Where(c => string.IsNullOrEmpty(query!.Code) || codes!.Contains(c.Code))
            .Where(c => string.IsNullOrEmpty(query?.Name) || c.Name.Contains(query!.Name, StringComparison.InvariantCultureIgnoreCase))
            .Where(c => !query!.EuTradeMember.HasValue || c.EuTradeMember == query.EuTradeMember)
            .Where(c => !query!.DevolvedAuthority.HasValue || c.DevolvedAuthority == query.DevolvedAuthority)
            .Where(c => !query!.LastUpdatedDate.HasValue || c.LastModifiedDate >= query.LastUpdatedDate.Value);

        var sortedResults = query.Order switch
        {
            "name" => query.Sort == "desc" ? results.OrderByDescending(c => c.Name) : results.OrderBy(c => c.Name),
            _ => query.Sort == "desc" ? results.OrderByDescending(c => c.Code) : results.OrderBy(c => c.Code)
        };

        return (
            sortedResults
            .Select(c => new CountryDTO
            {
                Name = c.Name,
                Code = c.Code,
                IdentifierId = c.IdentifierId,
                LongName = c.LongName,
                DevolvedAuthorityFlag = c.DevolvedAuthority,
                EuTradeMemberFlag = c.EuTradeMember,
                LastUpdatedDate = c.LastModifiedDate
            }).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(), items.Count);
    }
}