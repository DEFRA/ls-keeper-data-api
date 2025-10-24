using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class ProductionTypeLookupService : IProductionTypeLookupService
{
    /// <summary>
    /// To complete implementation when seeding is completed or to replace.
    /// </summary>
    /// <param name="lookupValue"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(string? productionTypeId, string? productionTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return (null, null);

        string? productionTypeId = null;
        string? productionTypeName = null;

        return await Task.FromResult((productionTypeId, productionTypeName));
    }
}
