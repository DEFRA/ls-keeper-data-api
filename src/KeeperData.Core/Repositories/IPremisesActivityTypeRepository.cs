using KeeperData.Core.Documents;

namespace KeeperData.Core.Repositories;

public interface IPremisesActivityTypeRepository
{
    Task<PremisesActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);

    Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}