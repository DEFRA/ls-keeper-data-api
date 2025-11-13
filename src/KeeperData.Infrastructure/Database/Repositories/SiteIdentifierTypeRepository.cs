using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class SiteIdentifierTypeRepository(
    IOptions<MongoConfig> config,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<SiteIdentifierTypeListDocument, SiteIdentifierTypeDocument>(config, client, unitOfWork),
        ISiteIdentifierTypeRepository
{
    public new async Task<SiteIdentifierTypeDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var allSiteIdentifierTypes = await GetAllAsync(cancellationToken);
        return allSiteIdentifierTypes.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(string? siteIdentifierTypeId, string? siteIdentifierTypeName)> FindAsync(string? lookupValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupValue))
            return (null, null);

        var allSiteIdentifierTypes = await GetAllAsync(cancellationToken);

        var siteIdentifierType = allSiteIdentifierTypes.FirstOrDefault(s =>
            s.Code.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));

        if (siteIdentifierType == null)
        {
            siteIdentifierType = allSiteIdentifierTypes.FirstOrDefault(s =>
                s.Name.Equals(lookupValue, StringComparison.OrdinalIgnoreCase));
        }

        return siteIdentifierType != null
            ? (siteIdentifierType.Code, siteIdentifierType.Name)
            : (null, null);
    }
}