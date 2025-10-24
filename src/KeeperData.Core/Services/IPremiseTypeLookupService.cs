namespace KeeperData.Core.Services;

public interface IPremiseTypeLookupService
{
    Task<(string? premiseTypeId, string? premiseTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}
