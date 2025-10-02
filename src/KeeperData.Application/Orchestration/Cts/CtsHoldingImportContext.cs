using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Source;

namespace KeeperData.Application.Orchestration.Cts;

public class CtsHoldingImportContext
{
    public required string Cph { get; init; }
    public CtsCphHolding? RawHolding { get; set; }
    public List<CtsAgentOrKeeper> RawAgents { get; set; } = [];
    public List<CtsAgentOrKeeper> RawKeepers { get; set; } = [];

    public CtsHoldingDocument? SilverHolding { get; set; }
    public List<CtsPartyDocument> SilverParties { get; set; } = [];

    public SiteDocument? GoldSite { get; set; }
    public List<PartyDocument> GoldParties { get; set; } = [];
}