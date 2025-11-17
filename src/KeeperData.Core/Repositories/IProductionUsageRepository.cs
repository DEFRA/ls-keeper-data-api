using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;

namespace KeeperData.Core.Repositories;

public interface IProductionUsageRepository : IReferenceDataRepository<ProductionUsageListDocument, ProductionUsageDocument>
{
    new Task<ProductionUsageDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default);
    Task<(string? productionUsageId, string? productionUsageDescription)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default);
}