using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Deletions;

public class SamHoldingDeleteContext
{
    public required string Cph { get; init; }
    public required int BatchId { get; init; }

    public List<SamHoldingDocument> SilverHoldings { get; set; } = [];
    public List<SiteDocument> GoldSites { get; set; } = [];
}
