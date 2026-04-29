using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class SiteTypeRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<SiteTypeListDocument, SiteTypeDocument>(mongoConfig, client, unitOfWork), ISiteTypeRepository
{
    public new async Task<SiteTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var siteTypes = await GetAllAsync(cancellationToken);
        return siteTypes.FirstOrDefault(x =>
            x.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(string? siteTypeId, string? siteTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
        {
            return (null, null);
        }

        var siteTypes = await GetAllAsync(cancellationToken);
        var match = siteTypes.FirstOrDefault(x =>
            x.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        return match != null
            ? (match.IdentifierId, match.Name)
            : (null, null);
    }
}