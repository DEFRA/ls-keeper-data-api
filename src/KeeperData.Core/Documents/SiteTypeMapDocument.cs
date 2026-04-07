using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// Represents a Site Type with its associated Site Activities.
/// Maps the relationship between a Site Type (e.g. Market, Slaughterhouse)
/// and the activities that can occur at that type of site.
/// </summary>
public class SiteTypeMapDocument : INestedEntity
{
    /// <summary>
    /// Unique identifier for this site type mapping.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The site type classification (code and name).
    /// </summary>
    [BsonElement("type")]
    [JsonPropertyName("type")]
    public required SiteTypeMapTypeInfo Type { get; set; }

    /// <summary>
    /// The activities associated with this site type. Empty if no activities apply.
    /// </summary>
    [BsonElement("activities")]
    [JsonPropertyName("activities")]
    public List<SiteTypeMapActivityInfo> Activities { get; set; } = [];
}

/// <summary>
/// Represents the type classification of a site (code and name).
/// </summary>
public class SiteTypeMapTypeInfo
{
    /// <summary>
    /// The business key/code for the site type.
    /// </summary>
    /// <example>MA</example>
    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The descriptive name of the site type.
    /// </summary>
    /// <example>Market</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

/// <summary>
/// Represents an activity that can occur at a site type.
/// </summary>
public class SiteTypeMapActivityInfo
{
    /// <summary>
    /// The business key/code for the site activity.
    /// </summary>
    /// <example>STM</example>
    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The descriptive name of the site activity.
    /// </summary>
    /// <example>Store Market</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}