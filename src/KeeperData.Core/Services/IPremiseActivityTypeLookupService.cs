namespace KeeperData.Core.Services;

public interface IPremiseActivityTypeLookupService
{
    Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}