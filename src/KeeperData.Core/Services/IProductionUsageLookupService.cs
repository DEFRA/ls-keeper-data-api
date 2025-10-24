namespace KeeperData.Core.Services;

public interface IProductionUsageLookupService
{
    Task<(string? productionUsageId, string? productionUsageName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}
