namespace KeeperData.Core.Services;

public interface IRoleTypeLookupService
{
    Task<(string? roleTypeId, string? roleTypeName)> FindRoleAsync(string lookupValue);
}
