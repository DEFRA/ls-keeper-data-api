namespace KeeperData.Core.Repositories;

public interface ISilverSitePartyRoleRelationshipRepository 
    : IGenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument>
{
    Task<List<string>> FindPartyIdsByHoldingIdentifierAsync(
        string holdingIdentifier,
        string source,
        CancellationToken cancellationToken = default);
}
