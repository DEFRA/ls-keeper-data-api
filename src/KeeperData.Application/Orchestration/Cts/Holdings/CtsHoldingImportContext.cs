using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Cts.Holdings;

public class CtsHoldingImportContext
{
    public required string Cph { get; init; }
    public int BatchId { get; init; }
    public DateTime CurrentDateTime { get; init; }

    public List<CtsCphHolding> RawHoldings { get; set; } = [];
    public List<CtsAgentOrKeeper> RawAgents { get; set; } = [];
    public List<CtsAgentOrKeeper> RawKeepers { get; set; } = [];

    public List<CtsHoldingDocument> SilverHoldings { get; set; } = [];
    public List<CtsPartyDocument> SilverParties { get; set; } = [];
    public List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> SilverPartyRoles { get; set; } = [];
}