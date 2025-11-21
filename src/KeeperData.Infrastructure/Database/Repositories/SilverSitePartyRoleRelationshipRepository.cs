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
}