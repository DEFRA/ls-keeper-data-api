using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface IPremiseActivityTypeLookupService
{
    Task<PremisesActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<PremisesActivityTypeDocument?> GetByCodeAsync(string? code, CancellationToken cancellationToken);

    Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}