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

    public async Task<(string? countryId, string? countryName)> FindAsync(string? countryCode, string? ukInternalCode, CancellationToken cancellationToken)
    {
        var searchKey = DetermineSearchKey(countryCode, ukInternalCode);
        return await _countryRepository.FindAsync(searchKey, cancellationToken);
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