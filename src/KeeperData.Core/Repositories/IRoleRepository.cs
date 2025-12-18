using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;

namespace KeeperData.Core.Repositories;

public interface IRoleRepository : IReferenceDataRepository<RoleListDocument, RoleDocument>
{
    new Task<RoleDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);

    Task<(string? roleId, string? roleCode, string? roleName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}