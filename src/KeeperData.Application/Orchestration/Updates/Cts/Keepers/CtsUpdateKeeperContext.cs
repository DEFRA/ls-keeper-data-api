using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Updates.Cts.Keepers;

public class CtsUpdateKeeperContext
{
    public required string PartyId { get; init; }
    public DateTime CurrentDateTime { get; init; }

    public CtsAgentOrKeeper? RawKeeper { get; set; }

    public CtsPartyDocument? SilverParty { get; set; }
    public List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> SilverPartyRoles { get; set; } = [];
}