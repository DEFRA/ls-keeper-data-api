using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class PremisesActivityTypeRepository(
    IOptions<MongoConfig> config,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<PremisesActivityTypeListDocument, PremisesActivityTypeDocument>(config, client, unitOfWork),
        IPremisesActivityTypeRepository
{
    public new async Task<PremisesActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var allPremisesActivityTypes = await GetAllAsync(cancellationToken);
        return allPremisesActivityTypes.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(string? premiseActivityTypeId, string? premiseActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null);

        var allPremisesActivityTypes = await GetAllAsync(cancellationToken);

        var premisesActivityType = allPremisesActivityTypes.FirstOrDefault(s =>
            s.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        if (premisesActivityType == null)
        {
            premisesActivityType = allPremisesActivityTypes.FirstOrDefault(s =>
                s.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));
        }

        return premisesActivityType != null
            ? (premisesActivityType.Code, premisesActivityType.Name)
            : (null, null);
    }
}