using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface ISpeciesTypeLookupService
{
    Task<SpeciesTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<(string? speciesTypeId, string? speciesTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}