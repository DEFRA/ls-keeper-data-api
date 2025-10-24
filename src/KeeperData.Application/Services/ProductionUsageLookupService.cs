using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class ProductionUsageLookupService : IProductionUsageLookupService
{
    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="lookupValue"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(string? productionUsageId, string? productionUsageName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return (null, null);

        string? productionUsageId = null;
        string? productionUsageName = null;

        return await Task.FromResult((productionUsageId, productionUsageName));
    }
}
