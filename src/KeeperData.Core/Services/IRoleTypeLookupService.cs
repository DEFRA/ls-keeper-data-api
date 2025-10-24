namespace KeeperData.Core.Services;

public interface IRoleTypeLookupService
{
    Task<(string? roleTypeId, string? roleTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}