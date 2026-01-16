using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class BaseHoldingDocument
{
    [JsonPropertyName("countyParishHoldingNumber")]
    [BsonElement("countyParishHoldingNumber")]
    [AutoIndexed]
    public string CountyParishHoldingNumber { get; set; } = string.Empty;

    [JsonPropertyName("alternativeHoldingIdentifier")]
    [BsonElement("alternativeHoldingIdentifier")]
    [AutoIndexed]
    public string? AlternativeHoldingIdentifier { get; set; }

    [JsonPropertyName("cphTypeIdentifier")]
    [BsonElement("cphTypeIdentifier")]
    public string CphTypeIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("locationName")]
    [BsonElement("locationName")]
    [AutoIndexed]
    public string? LocationName { get; set; }

    [JsonPropertyName("holdingStartDate")]
    [BsonElement("holdingStartDate")]
    public DateTime HoldingStartDate { get; set; } = default;

    [JsonPropertyName("holdingEndDate")]
    [BsonElement("holdingEndDate")]
    public DateTime? HoldingEndDate { get; set; }

    [JsonPropertyName("holdingStatus")]
    [BsonElement("holdingStatus")]
    public string? HoldingStatus { get; set; }

    [JsonPropertyName("premiseActivityTypeId")]
    [BsonElement("premiseActivityTypeId")]
    public string? PremiseActivityTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("premiseActivityTypeCode")]
    [BsonElement("premiseActivityTypeCode")]
    public string? PremiseActivityTypeCode { get; set; }

    [JsonPropertyName("premiseTypeIdentifier")]
    [BsonElement("premiseTypeIdentifier")]
    public string? PremiseTypeIdentifier { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("premiseTypeCode")]
    [BsonElement("premiseTypeCode")]
    public string? PremiseTypeCode { get; set; }

    [JsonPropertyName("location")]
    [BsonElement("location")]
    public LocationDocument? Location { get; set; }

    [JsonPropertyName("communication")]
    [BsonElement("communication")]
    public CommunicationDocument? Communication { get; set; }

    [JsonPropertyName("groupMarks")]
    [BsonElement("groupMarks")]
    public List<GroupMarkDocument>? GroupMarks { get; set; } = [];
}