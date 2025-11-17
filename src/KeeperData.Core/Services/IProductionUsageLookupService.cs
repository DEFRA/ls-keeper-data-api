using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface IProductionUsageLookupService
{
    Task<ProductionUsageDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<(string? productionUsageId, string? productionUsageName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}