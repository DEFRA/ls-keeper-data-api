using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class ProductionUsageRepository(
    IOptions<MongoConfig> config,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<ProductionUsageListDocument, ProductionUsageDocument>(config, client, unitOfWork),
        IProductionUsageRepository
{
    public new async Task<ProductionUsageDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var allProductionUsages = await GetAllAsync(cancellationToken);
        return allProductionUsages.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(string? productionUsageId, string? productionUsageDescription)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null);

        var allProductionUsages = await GetAllAsync(cancellationToken);

        var productionUsage = allProductionUsages.FirstOrDefault(s =>
            s.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        if (productionUsage == null)
        {
            productionUsage = allProductionUsages.FirstOrDefault(s =>
                s.Description.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));
        }

        return productionUsage != null
            ? (productionUsage.Code, productionUsage.Description)
            : (null, null);
    }
}