using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Documents.Working;

namespace KeeperData.Application.Orchestration.Imports.Sam.Holdings;

public class SamHoldingImportContext
{
    public required string Cph { get; init; }
    public int BatchId { get; init; }
    public DateTime CurrentDateTime { get; init; }

    public List<SamCphHolding> RawHoldings { get; set; } = [];
    public List<SamHerd> RawHerds { get; set; } = [];
    public List<SamCphHolder> RawHolders { get; set; } = [];
    public List<SamParty> RawParties { get; set; } = [];

    public List<SamHoldingDocument> SilverHoldings { get; set; } = [];
    public List<SamPartyDocument> SilverParties { get; set; } = [];
    public List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> SilverPartyRoles { get; set; } = [];
    public List<SamHerdDocument> SilverHerds { get; set; } = [];

    public string GoldSiteId { get; set; } = string.Empty;
    public SiteDocument? ExistingGoldSite { get; set; }
    public List<string> ExistingGoldPartyIds { get; set; } = [];

    public SiteDocument? GoldSite { get; set; }
    public List<PartyDocument> GoldParties { get; set; } = [];
    public List<Core.Documents.SitePartyRoleRelationshipDocument> GoldSitePartyRoles { get; set; } = [];
    public List<SiteGroupMarkRelationshipDocument> GoldSiteGroupMarks { get; set; } = [];

    public List<SitePartyRoleRelationship> PartiesWithNoRelationshipToSiteToClean { get; set; } = [];
}