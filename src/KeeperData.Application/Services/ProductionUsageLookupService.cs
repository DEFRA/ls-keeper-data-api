using KeeperData.Core.Documents;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class ProductionUsageLookupService(IReferenceDataCache cache)
    : IProductionUsageLookupService
{
    public Task<ProductionUsageDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<ProductionUsageDocument?>(null);

        var match = cache.ProductionUsages.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }

    public Task<(string? productionUsageId, string? productionUsageName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(lookupValue) || lookupValue == "-")
            return Task.FromResult<(string?, string?)>((null, null));

        var match = cache.ProductionUsages.FirstOrDefault(s =>
            s.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        match ??= cache.ProductionUsages.FirstOrDefault(s =>
            s.Description.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match != null
            ? (match.IdentifierId, match.Description)
            : ((string?)null, (string?)null));
    }
}