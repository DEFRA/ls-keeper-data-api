using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class RoleTypeLookupService : IRoleTypeLookupService
{
    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public async Task<(string? roleTypeId, string? roleTypeName)> FindRoleAsync(string lookupValue, CancellationToken cancellationToken)
    {
        string? roleTypeId = null;
        string? roleTypeName = null;

        return await Task.FromResult((roleTypeId, roleTypeName));
    }
}