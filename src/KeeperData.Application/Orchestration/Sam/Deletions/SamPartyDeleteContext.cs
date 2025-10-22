using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Deletions;

public class SamPartyDeleteContext
{
    public required string PartyId { get; init; }
    public int BatchId { get; init; }

    public SamPartyDocument? SilverParty { get; set; }
    public PartyDocument? GoldParty { get; set; }
}