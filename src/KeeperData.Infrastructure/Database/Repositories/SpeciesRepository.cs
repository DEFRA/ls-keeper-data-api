using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class SpeciesRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<SpeciesListDocument, SpeciesDocument>(mongoConfig, client, unitOfWork), ISpeciesRepository
{
    public new async Task<SpeciesDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var species = await GetAllAsync(cancellationToken);
        return species.FirstOrDefault(s => s.IdentifierId?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);
    }

    public async Task<(string? speciesId, string? speciesName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null);

        var species = await GetAllAsync(cancellationToken);

        // Try exact match on Code first (case-insensitive)
        var speciesItem = species.FirstOrDefault(s => s.Code?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        // If not found by code, try name match
        speciesItem ??= species.FirstOrDefault(s => s.Name?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        return speciesItem != null
            ? (speciesItem.Code, speciesItem.Name)
            : (null, null);
    }
}