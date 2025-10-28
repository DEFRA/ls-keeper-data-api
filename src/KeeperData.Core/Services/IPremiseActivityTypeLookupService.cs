using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface IPremiseActivityTypeLookupService
{
    Task<PremiseActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}