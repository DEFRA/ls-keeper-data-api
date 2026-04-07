using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class SiteTypeMapRepository(
    IOptions<MongoConfig> config,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : ReferenceDataRepository<SiteTypeMapListDocument, SiteTypeMapDocument>(config, client, unitOfWork),
        ISiteTypeMapRepository
{
    public new async Task<SiteTypeMapDocument?> GetByIdAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var allSiteTypeMaps = await GetAllAsync(cancellationToken);
        return allSiteTypeMaps.FirstOrDefault(s =>
            s.IdentifierId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<SiteTypeMapDocument?> FindByTypeCodeAsync(string? typeCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
            return null;

        var allSiteTypeMaps = await GetAllAsync(cancellationToken);
        return allSiteTypeMaps.FirstOrDefault(s =>
            s.Type.Code.Equals(typeCode, StringComparison.OrdinalIgnoreCase));
    }
}