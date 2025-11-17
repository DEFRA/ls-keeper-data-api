using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface ICountryIdentifierLookupService
{
    Task<CountryDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<(string? countryId, string? countryName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}