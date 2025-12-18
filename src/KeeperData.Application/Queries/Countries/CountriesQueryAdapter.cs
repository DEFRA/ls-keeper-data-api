using System.Collections.Immutable;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Countries;

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
            .Where(c => String.IsNullOrEmpty(query!.Code) || codes!.Contains(c.Code))
            .Where(c => String.IsNullOrEmpty(query?.Name) || c.Name == query!.Name)
            .Where(c => !query!.EuTradeMember.HasValue || c.EuTradeMember == query.EuTradeMember)
            .Where(c => !query!.DevolvedAuthority.HasValue || c.DevolvedAuthority == query.DevolvedAuthority);

        return (
            results
            .Select(c => new CountryDTO
            {
                Name = c.Name,
                Code = c.Code,
                IdentifierId = c.IdentifierId,
                LongName = c.LongName,
                DevolvedAuthorityFlag = c.DevolvedAuthority,
                EuTradeMemberFlag = c.EuTradeMember,
                LastUpdatedDate = c.LastModifiedDate
            }).ToList(), items.Count());
    }
}