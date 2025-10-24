namespace KeeperData.Core.Services;

public interface ICountryIdentifierLookupService
{
    Task<(string? countryId, string? countryName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}
