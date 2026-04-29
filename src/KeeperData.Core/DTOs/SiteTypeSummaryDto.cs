using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// The type of site an animal may reside at.
/// </summary>
public class SiteTypeSummaryDto : ISummaryDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public string IdentifierId { get; set; } = string.Empty;

    /// <summary>
    /// The business key/code values for a siteType.
    /// </summary>
    /// <example>AH</example>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The type of site an animal may reside at.
    /// </summary>
    /// <example>Agricultural Holding</example>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the last time the SiteType record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}