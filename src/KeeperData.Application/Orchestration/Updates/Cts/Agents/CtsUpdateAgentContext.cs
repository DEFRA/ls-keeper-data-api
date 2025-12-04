using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Updates.Cts.Agents;

public class CtsUpdateAgentContext
{
    public required string PartyId { get; init; }
    public DateTime CurrentDateTime { get; init; }

    public CtsAgentOrKeeper? RawAgent { get; set; }

    public CtsPartyDocument? SilverParty { get; set; }
}