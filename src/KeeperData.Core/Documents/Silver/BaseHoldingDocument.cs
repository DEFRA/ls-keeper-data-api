namespace KeeperData.Core.Documents.Silver;

public class BaseHoldingDocument
{
    public string CountyParishHoldingNumber { get; set; } = string.Empty;
    public string? AlternativeHoldingIdentifier { get; set; }
    public string CphTypeIdentifier { get; set; } = string.Empty;

    public DateTime HoldingStartDate { get; set; } = default;
    public DateTime? HoldingEndDate { get; set; }

    public string? HoldingStatus { get; set; }
    
    public string? PremiseActivityTypeId { get; set; } // LOV Lookup
    public string? PremiseActivityTypeCode { get; set; }
    
    public string? PremiseTypeIdentifier { get; set; } // LOV Lookup
    public string? PremiseTypeCode { get; set; }
    
    public string? LocationName { get; set; }

    public LocationDocument? Location { get; set; }

    public CommunicationDocument? Communication { get; set; }

    public List<GroupMarkDocument>? GroupMarks { get; set; } = [];
}
