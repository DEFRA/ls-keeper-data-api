using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class CountryIdentifierLookupService : ICountryIdentifierLookupService
{
    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="lookupValue"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(string? countryId, string? countryName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return (null, null);

        string? countryId = null;
        string? countryName = null;

        return await Task.FromResult((countryId, countryName));
    }
}