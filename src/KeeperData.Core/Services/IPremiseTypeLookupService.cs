using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface IPremiseTypeLookupService
{
    Task<PremisesTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<(string? premiseTypeId, string? premiseTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}