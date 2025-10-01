using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Source;

namespace KeeperData.Application.Orchestration.Sam;

public class SamHoldingImportContext
{
    public required string Cph { get; init; }
    public SamCphHolding? Raw { get; set; }
    public SamHoldingDocument? SilverHolding { get; set; }
    public List<SamPartyDocument> SilverParties { get; set; } = [];
    public SiteDocument? GoldSite { get; set; }
    public List<PartyDocument> GoldParties { get; set; } = [];
}
