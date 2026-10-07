using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteActivityDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The type of site activity.
    /// </summary>
    [BsonElement("type")]
    [JsonPropertyName("type")]
    public required SiteActivityTypeSummaryDocument Type { get; set; }

    /// <summary>
    /// The start date of the activity.
    /// </summary>
    [BsonElement("startDate")]
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end date of the activity.
    /// </summary>
    [BsonElement("endDate")]
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The timestamp of the last time the SiteActivity record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}