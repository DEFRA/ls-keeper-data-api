namespace KeeperData.Core.Documents.Working;

public class SiteGroupMarkRelationship
{
    public string? Id { get; set; }

    public required string Herdmark { get; set; }

    public required string CountyParishHoldingHerd { get; set; }

    public required string HoldingIdentifier { get; set; }

    public required string PartyId { get; set; }

    public string? ProductionUsageCode { get; set; }
}