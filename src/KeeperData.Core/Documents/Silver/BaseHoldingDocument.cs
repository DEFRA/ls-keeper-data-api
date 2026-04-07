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

    [JsonPropertyName("siteActivityTypeId")]
    [BsonElement("siteActivityTypeId")]
    public string? SiteActivityTypeId { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("siteActivityTypeCode")]
    [BsonElement("siteActivityTypeCode")]
    public string? SiteActivityTypeCode { get; set; }

    [JsonPropertyName("siteTypeIdentifier")]
    [BsonElement("siteTypeIdentifier")]
    public string? SiteTypeIdentifier { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("siteTypeCode")]
    [BsonElement("siteTypeCode")]
    public string? SiteTypeCode { get; set; }

    [JsonPropertyName("location")]
    [BsonElement("location")]
    public LocationDocument? Location { get; set; }

    [JsonPropertyName("communication")]
    [BsonElement("communication")]
    public CommunicationDocument? Communication { get; set; }
}