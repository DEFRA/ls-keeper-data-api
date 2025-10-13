using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Inserts;

public class SamHoldingInsertContext
{
    public required string Cph { get; init; }
    public required int BatchId { get; init; }

    public SamCphHolding? RawHolding { get; set; }
    public List<SamCphHolder> RawHolders { get; set; } = [];
    public List<SamParty> RawParties { get; set; } = [];
    public List<SamHerd> RawHerds { get; set; } = [];

    public SamHoldingDocument? SilverHolding { get; set; }
    public List<SamPartyDocument> SilverParties { get; set; } = [];
    public List<PartyRoleRelationshipDocument> SilverPartyRoles { get; set; } = [];

    public SiteDocument? GoldSite { get; set; }
    public List<PartyDocument> GoldParties { get; set; } = [];
}