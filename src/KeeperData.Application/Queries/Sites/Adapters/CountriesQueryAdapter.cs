using System.Runtime.InteropServices;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Queries.Sites.Adapters;

public class CountriesQueryAdapter(ICountryRepository repository)
{
    private readonly ICountryRepository _repository = repository;

    public async Task<(List<CountryDTO> Items, int TotalCount)> GetCountriesAsync(
        GetCountriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync();

        var results = items.Where(c => String.IsNullOrEmpty(query.Code) || c.Code == query.Code).ToList();
        return (
            results
            .Select(c => new CountryDTO 
        { Name = c.Name, Code = c.Code, IdentifierId = c.IdentifierId, 
        LongName = c.LongName, DevolvedAuthorityFlag = c.DevolvedAuthority, 
        EuTradeMemberFlag = c.EuTradeMember,
        LastUpdatedDate = c.LastModifiedDate
         }).ToList(), items.Count());
    }
}