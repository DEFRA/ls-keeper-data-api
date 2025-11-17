using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;

namespace KeeperData.Application.Services;

public class ProductionUsageLookupService(IProductionUsageRepository productionUsageRepository)
    : IProductionUsageLookupService
{
    public async Task<ProductionUsageDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken)
    {
        return await productionUsageRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<(string? productionUsageId, string? productionUsageName)> FindAsync(string? lookupValue, CancellationToken cancellationToken)
    {
        return await productionUsageRepository.FindAsync(lookupValue, cancellationToken);
    }
}