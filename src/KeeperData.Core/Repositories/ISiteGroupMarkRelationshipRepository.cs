using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Working;

namespace KeeperData.Core.Repositories;

public interface ISiteGroupMarkRelationshipRepository : IGenericRepository<SiteGroupMarkRelationshipDocument>
{
    Task<List<SiteGroupMarkRelationship>> GetExistingSiteGroupMarkRelationships(
        List<string> partyIds,
        string holdingIdentifier,
        CancellationToken cancellationToken = default);
}