using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Cts.Deletions;

public class CtsKeeperDeleteContext
{
    public required string PartyId { get; init; }
    public required int BatchId { get; init; }

    public CtsPartyDocument? SilverParty { get; set; }
}
