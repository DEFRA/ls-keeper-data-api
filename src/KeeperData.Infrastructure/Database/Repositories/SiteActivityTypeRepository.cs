using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class SiteActivityTypeRepository(
    IOptions<MongoConfig> config,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<SiteActivityTypeListDocument, SiteActivityTypeDocument>(config, client, unitOfWork),
        ISiteActivityTypeRepository
{
    public new async Task<SiteActivityTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var allSiteActivityTypes = await GetAllAsync(cancellationToken);
        return allSiteActivityTypes.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(string? siteActivityTypeId, string? siteActivityTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null);

        var allSiteActivityTypes = await GetAllAsync(cancellationToken);

        var siteActivityType = allSiteActivityTypes.FirstOrDefault(s =>
            s.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        if (siteActivityType == null)
        {
            siteActivityType = allSiteActivityTypes.FirstOrDefault(s =>
                s.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));
        }

        return siteActivityType != null
            ? (siteActivityType.IdentifierId, siteActivityType.Name)
            : (null, null);
    }
}