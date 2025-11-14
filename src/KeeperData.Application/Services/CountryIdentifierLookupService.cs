using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class CountryIdentifierLookupService : ICountryIdentifierLookupService
{
    private readonly ICountryRepository _countryRepository;

    public CountryIdentifierLookupService(ICountryRepository countryRepository)
    {
        _countryRepository = countryRepository;
    }

    public async Task<CountryDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        return await _countryRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<(string? countryId, string? countryName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        return await _countryRepository.FindAsync(lookupValue, cancellationToken);
    }
}