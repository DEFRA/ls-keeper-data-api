using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Cts.Deletions;

public class CtsHoldingDeleteContext
{
    public required string Cph { get; init; }
    public int BatchId { get; init; }

    public List<CtsHoldingDocument> SilverHoldings { get; set; } = [];
}