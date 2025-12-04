using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Application.Orchestration.Updates.Cts.Holdings;

public class CtsUpdateHoldingContext
{
    public required string Cph { get; init; }
    public string CphTrimmed => Cph.LidIdentifierToCph();
    public DateTime CurrentDateTime { get; init; }

    // Singular Holding
    public CtsCphHolding? RawHolding { get; set; }

    // Lists for related entities (One holding has many agents/keepers)
    public List<CtsAgentOrKeeper> RawAgents { get; set; } = [];
    public List<CtsAgentOrKeeper> RawKeepers { get; set; } = [];

    // Singular Silver Document
    public CtsHoldingDocument? SilverHolding { get; set; }

    public List<CtsPartyDocument> SilverParties { get; set; } = [];
}