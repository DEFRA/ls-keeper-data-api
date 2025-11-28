using KeeperData.Core.Documents.Working;

namespace KeeperData.Core.Repositories;

public interface IGoldSitePartyRoleRelationshipRepository
    : IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>
{
    Task<List<SitePartyRoleRelationship>> GetExistingSitePartyRoleRelationships(
        List<string> holderPartyIds,
        string holderRoleId,
        CancellationToken cancellationToken = default);
}
