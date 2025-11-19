using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class SilverSitePartyRoleRelationshipRepository(
    IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : GenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument>(
        mongoConfig,
        client,
        unitOfWork), ISilverSitePartyRoleRelationshipRepository
{
    public async Task<List<string>> FindPartyIdsByHoldingIdentifierAsync(
        string holdingIdentifier,
        string source,
        bool isHolder,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.And(
            Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier),
            Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.Source, source),
            Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.IsHolder, isHolder)
        );

        var projection = Builders<Core.Documents.Silver.SitePartyRoleRelationshipDocument>.Projection
            .Include(x => x.PartyId);

        var result = await _collection
            .Find(filter)
            .Project(projection)
            .ToListAsync(cancellationToken);

        return [.. result.Select(doc => doc.GetValue("partyId").AsString)];
    }
}