using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface IRoleTypeLookupService
{
    Task<RoleDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<(string? roleTypeId, string? roleTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}