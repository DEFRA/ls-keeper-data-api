using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Working;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace KeeperData.Infrastructure.Database.Repositories;

public class SiteGroupMarkRelationshipRepository(IOptions<MongoConfig> mongoConfig,
    IMongoClient client,
    IUnitOfWork unitOfWork)
    : GenericRepository<SiteGroupMarkRelationshipDocument>(
        mongoConfig,
        client,
        unitOfWork), ISiteGroupMarkRelationshipRepository
{
    public async Task<List<SiteGroupMarkRelationship>> GetExistingSiteGroupMarkRelationships(
        List<string> partyIds,
        string holdingIdentifier,
        CancellationToken cancellationToken = default)
    {
        if (partyIds == null)
            return [];

        var filter = Builders<SiteGroupMarkRelationshipDocument>.Filter.In(x => x.PartyId, partyIds) &
            Builders<SiteGroupMarkRelationshipDocument>.Filter.Eq(x => x.HoldingIdentifier, holdingIdentifier);

        var projection = Builders<SiteGroupMarkRelationshipDocument>.Projection
            .Include(x => x.Id)
            .Include(x => x.Herdmark)
            .Include(x => x.CountyParishHoldingHerd)
            .Include(x => x.HoldingIdentifier)
            .Include(x => x.PartyId)
            .Include(x => x.ProductionUsageCode);

        var result = await _collection
            .Find(filter)
            .Project(projection)
            .ToListAsync(cancellationToken);

        var results = result
            .Select(doc => new SiteGroupMarkRelationship
            {
                Id = doc.GetValue("id").AsString,
                Herdmark = doc.GetValue("herdmark").AsString,
                CountyParishHoldingHerd = doc.GetValue("countyParishHoldingHerd").AsString,
                HoldingIdentifier = doc.GetValue("holdingIdentifier").AsString,
                PartyId = doc.GetValue("partyId").AsString,
                ProductionUsageCode = doc.GetValue("productionUsageCode").AsString
            })
            .ToList();

        return results;
    }
}