using KeeperData.Core.Documents.Working;

namespace KeeperData.Core.Repositories;

public interface IGoldSitePartyRoleRelationshipRepository
    : IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>
{
    Task<List<SitePartyRoleRelationship>> GetExistingSitePartyRoleRelationships(
        string holdingIdentifier,
        CancellationToken cancellationToken = default);
}