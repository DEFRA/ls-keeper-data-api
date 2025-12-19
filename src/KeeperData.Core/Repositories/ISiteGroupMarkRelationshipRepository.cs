using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Working;

namespace KeeperData.Core.Repositories;

public interface ISiteGroupMarkRelationshipRepository : IGenericRepository<SiteGroupMarkRelationshipDocument>
{
    Task<List<SiteGroupMarkRelationship>> GetExistingSiteGroupMarkRelationships(
        List<string> customerNumbers,
        string holdingIdentifier,
        CancellationToken cancellationToken = default);
}