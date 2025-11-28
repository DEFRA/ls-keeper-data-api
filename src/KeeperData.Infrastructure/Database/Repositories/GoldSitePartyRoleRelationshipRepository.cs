using KeeperData.Core.Documents.Working;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class GoldSitePartyRoleRelationshipRepository(IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : GenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>(
        mongoConfig,
        client,
        unitOfWork), IGoldSitePartyRoleRelationshipRepository
{
    public async Task<List<SitePartyRoleRelationship>> GetExistingSitePartyRoleRelationships(
        List<string> holderPartyIds,
        string holderRoleId,
        CancellationToken cancellationToken = default)
    {
        if (holderPartyIds == null)
            return [];

        var filter = Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.In(x => x.PartyId, holderPartyIds) &
            Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Filter.Eq(x => x.RoleTypeId, holderRoleId);

        var projection = Builders<Core.Documents.SitePartyRoleRelationshipDocument>.Projection
            .Include(x => x.Id)
            .Include(x => x.PartyId)
            .Include(x => x.HoldingIdentifier);

        var result = await _collection
            .Find(filter)
            .Project(projection)
            .ToListAsync(cancellationToken);

        var results = result
            .Select(doc => new SitePartyRoleRelationship
            {
                Id = doc.GetValue("id").AsString,
                PartyId = doc.GetValue("partyId").AsString,
                HoldingIdentifier = doc.GetValue("holdingIdentifier").AsString,
                RoleTypeId = doc.GetValue("roleTypeId").AsString
            })
            .ToList();

        return results;
    }
}
