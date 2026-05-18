using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class AssociatedHoldingRelationship
{
    [JsonPropertyName("holdingIdentifier")]
    [BsonElement("holdingIdentifier")]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("contiguousFlag")]
    [BsonElement("contiguousFlag")]
    public bool ContiguousFlag { get; set; }

    [JsonPropertyName("startDate")]
    [BsonElement("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    [BsonElement("endDate")]
    public string? EndDate { get; set; }
}