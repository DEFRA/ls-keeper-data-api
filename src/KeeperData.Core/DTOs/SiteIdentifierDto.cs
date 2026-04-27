using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// An identifier associated with a site (e.g. CPH Number).
/// </summary>
public class SiteIdentifierDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The site identifier value (e.g. CPH Number).
    /// </summary>
    /// <example>57/103/2335</example>
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = default!;

    /// <summary>
    /// The type of the site identifier.
    /// </summary>
    [JsonPropertyName("type")]
    public required SiteIdentifierTypeDto Type { get; set; }

    /// <summary>
    /// The timestamp of the last time the SiteIdentifier record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}

/// <summary>
/// The type which identifies the site identifier.
/// </summary>
public class SiteIdentifierTypeDto: ISummaryDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public string IdentifierId { get; set; }

    /// <summary>
    /// The business key/code values for a Site Identifier Type.
    /// </summary>
    /// <example>CPHN</example>
    [JsonPropertyName("code")]
    public string Code { get; set; }

    /// <summary>
    /// The type which identifies the site identifier.
    /// </summary>
    /// <example>CPH Number</example>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The timestamp of the last time the SiteIdentifier Type record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}