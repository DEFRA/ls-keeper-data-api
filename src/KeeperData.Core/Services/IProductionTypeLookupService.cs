using KeeperData.Core.Documents;

namespace KeeperData.Core.Services;

public interface IProductionTypeLookupService
{
    Task<ProductionTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken);

    Task<(string? productionTypeId, string? productionTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken);
}