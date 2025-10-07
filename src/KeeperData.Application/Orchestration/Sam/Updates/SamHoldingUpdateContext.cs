using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Updates;

public class SamHoldingUpdateContext
{
    public required string Cph { get; init; }
    public required int BatchId { get; init; }

    public SamCphHolding? RawHolding { get; set; }
    public SamHoldingDocument? SilverHolding { get; set; }
    public SiteDocument? GoldSite { get; set; }
}