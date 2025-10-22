using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Cts.Updates;

public class CtsHoldingUpdateContext
{
    public required string Cph { get; init; }
    public int BatchId { get; init; }

    public CtsCphHolding? RawHolding { get; set; }
    public CtsHoldingDocument? SilverHolding { get; set; }
    public SiteDocument? GoldSite { get; set; }
}