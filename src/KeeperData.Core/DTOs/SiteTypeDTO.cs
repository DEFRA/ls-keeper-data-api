using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A Site Type with its associated Site Activities.
/// </summary>
public class SiteTypeDTO
{
    /// <summary>
    /// Unique identifier for this site type.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The site type classification (code and name).
    /// </summary>
    [JsonPropertyName("type")]
    public required SiteTypeInfoDTO Type { get; set; }

    /// <summary>
    /// The activities associated with this site type. Empty if no activities apply.
    /// </summary>
    [JsonPropertyName("activities")]
    public List<SiteActivityInfoDTO> Activities { get; set; } = [];
}

/// <summary>
/// Represents the type classification of a site (code and name).
/// </summary>
public class SiteTypeInfoDTO
{
    /// <summary>
    /// The business key/code for the site type.
    /// </summary>
    /// <example>MA</example>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The descriptive name of the site type.
    /// </summary>
    /// <example>Market</example>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

/// <summary>
/// Represents an activity that can occur at a site type.
/// </summary>
public class SiteActivityInfoDTO
{
    /// <summary>
    /// The business key/code for the site activity.
    /// </summary>
    /// <example>STM</example>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The descriptive name of the site activity.
    /// </summary>
    /// <example>Store Market</example>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}