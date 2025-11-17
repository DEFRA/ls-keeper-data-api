using KeeperData.Core.Documents;

namespace KeeperData.Core.Repositories;

public interface IPremisesTypeRepository
{
    Task<PremisesTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);
    Task<(string? premiseTypeId, string? premiseTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}