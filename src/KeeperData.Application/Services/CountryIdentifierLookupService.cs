using KeeperData.Core.Documents;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class CountryIdentifierLookupService(IReferenceDataCache cache) : ICountryIdentifierLookupService
{
    public Task<CountryDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<CountryDocument?>(null);

        var country = cache.Countries.FirstOrDefault(c =>
            c.IdentifierId?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);

        return Task.FromResult(country);
    }

    public Task<(string? countryId, string? countryCode, string? countryName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        return Task.FromResult(FindInCache(lookupValue));
    }

    public Task<(string? countryId, string? countryCode, string? countryName)> FindAsync(string? countryCode, string? ukInternalCode, CancellationToken cancellationToken)
    {
        var searchKey = DetermineSearchKey(countryCode, ukInternalCode);
        return Task.FromResult(FindInCache(searchKey));
    }

    private (string? countryId, string? countryCode, string? countryName) FindInCache(string? lookupValue)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null, null);

        var country = cache.Countries.FirstOrDefault(c =>
            c.Code?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        country ??= cache.Countries.FirstOrDefault(c =>
            c.Name?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        return country != null
            ? (country.IdentifierId, country.Code, country.Name)
            : (null, null, null);
    }

    private static string? DetermineSearchKey(string? countryCode, string? ukInternalCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return null;

        if (string.Equals(countryCode, "GB", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(ukInternalCode))
        {
            return ukInternalCode.ToUpperInvariant().Trim() switch
            {
                "ENGLAND" => "GB-ENG",
                "WALES" => "GB-WLS",
                "SCOTLAND" => "GB-SCT",
                "NORTHERN IRELAND" => "GB-NIR",
                _ => countryCode
            };
        }

        return countryCode;
    }
}