using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Holdings;

public class SamHoldingImportContext
{
    public required string Cph { get; init; }
    public int BatchId { get; init; }
    public DateTime CurrentDateTime { get; init; }

    public List<SamCphHolding> RawHoldings { get; set; } = [];
    public List<SamCphHolder> RawHolders { get; set; } = [];
    public List<SamHerd> RawHerds { get; set; } = [];
    public List<SamParty> RawParties { get; set; } = [];

    public List<SamHoldingDocument> SilverHoldings { get; set; } = [];
    public List<SamPartyDocument> SilverParties { get; set; } = [];
    public List<PartyRoleRelationshipDocument> SilverPartyRoles { get; set; } = [];
    public List<SamHerdDocument> SilverHerds { get; set; } = [];

    public SiteDocument? GoldSite { get; set; }
    public List<PartyDocument> GoldParties { get; set; } = [];
    public List<SitePartyRoleRelationshipDocument> GoldSitePartyRoles { get; set; } = [];
    public List<SiteGroupMarkRelationshipDocument> GoldSiteGroupMarks { get; set; } = [];
}