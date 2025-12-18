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

        return (
            items
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