using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class RoleTypeLookupService(IReferenceDataCache cache) : IRoleTypeLookupService
{
    public Task<RoleDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<RoleDocument?>(null);

        var role = cache.Roles.FirstOrDefault(r =>
            r.IdentifierId?.Equals(id, StringComparison.OrdinalIgnoreCase) == true);

        return Task.FromResult(role);
    }

    public Task<(string? roleTypeId, string? roleTypeCode, string? roleTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return Task.FromResult<(string?, string?, string?)>((null, null, null));

        var role = cache.Roles.FirstOrDefault(r =>
            r.Code?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        role ??= cache.Roles.FirstOrDefault(r =>
            r.Name?.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) == true);

        return Task.FromResult(role != null
            ? (role.IdentifierId, role.Code, role.Name)
            : ((string?)null, (string?)null, (string?)null));
    }
}