using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Holders;

public class SamHolderImportContext
{
    public required string PartyId { get; init; }
    public int BatchId { get; init; }
    public DateTime CurrentDateTime { get; init; }

    public List<SamCphHolder> RawHolders { get; set; } = [];

    public List<SamPartyDocument> SilverParties { get; set; } = [];
    public List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> SilverPartyRoles { get; set; } = [];

    public List<PartyDocument> GoldParties { get; set; } = [];
    public List<Core.Documents.SitePartyRoleRelationshipDocument> GoldSitePartyRoles { get; set; } = [];
}