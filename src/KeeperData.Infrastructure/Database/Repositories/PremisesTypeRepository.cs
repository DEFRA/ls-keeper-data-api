using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class PremisesTypeRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<PremisesTypeListDocument, PremisesTypeDocument>(mongoConfig, client, unitOfWork), IPremisesTypeRepository
{
    public new async Task<PremisesTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var premisesTypes = await GetAllAsync(cancellationToken);
        return premisesTypes.FirstOrDefault(x =>
            x.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(string? premiseTypeId, string? premiseTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
        {
            return (null, null);
        }

        var premisesTypes = await GetAllAsync(cancellationToken);
        var match = premisesTypes.FirstOrDefault(x =>
            x.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        return match != null
            ? (match.IdentifierId, match.Name)
            : (null, null);
    }
}