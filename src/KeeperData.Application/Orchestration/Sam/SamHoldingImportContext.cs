using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Source;

namespace KeeperData.Application.Orchestration.Sam;

public class SamHoldingImportContext
{
    public required string Cph { get; init; }

    public SamCphHolding? RawHolding { get; set; }
    public List<SamCphHolder> RawHolders { get; set; } = [];
    public List<SamParty> RawParties { get; set; } = [];
    public List<SamHerd> RawHerds { get; set; } = [];

    public SamHoldingDocument? SilverHolding { get; set; }
    public List<SamPartyDocument> SilverParties { get; set; } = [];

    public SiteDocument? GoldSite { get; set; }
    public List<PartyDocument> GoldParties { get; set; } = [];
}