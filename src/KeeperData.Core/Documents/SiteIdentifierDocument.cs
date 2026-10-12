using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteIdentifierDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The site identifier value (e.g. CPH Number).
    /// </summary>
    /// <example>57/103/2335</example>
    [BsonElement("identifier")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = default!;

    /// <summary>
    /// The type of the site identifier.
    /// </summary>
    [BsonElement("type")]
    [JsonPropertyName("type")]
    public SiteIdentifierSummaryDocument Type { get; set; } = default!;

    /// <summary>
    /// The timestamp of the last time the SiteIdentifier record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}