using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class CountryRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<CountryListDocument, CountryDocument>(mongoConfig, client, unitOfWork), ICountryRepository
{
    public new async Task<CountryDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var countries = await GetAllAsync(cancellationToken);
        return countries.FirstOrDefault(c => c.IdentifierId?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<(string? countryId, string? countryName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null);

        var countries = await GetAllAsync(cancellationToken);

        // Try exact match on Code first (case-insensitive)
        var country = countries.FirstOrDefault(c => c.Code?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        // If not found by code, try name match
        country ??= countries.FirstOrDefault(c => c.Name?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        return country != null
            ? (country.Code, country.Name)
            : (null, null);
    }
}