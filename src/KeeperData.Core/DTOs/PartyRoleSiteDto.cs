using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A lightweight site summary embedded within a party's role.
/// </summary>
public class PartyRoleSiteDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The name of the site.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The type of site.
    /// </summary>
    [JsonPropertyName("type")]
    public SiteTypeSummaryDto? Type { get; set; }

    /// <summary>
    /// The current state of the site.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// The identifiers associated with this site.
    /// </summary>
    [JsonPropertyName("identifiers")]
    public List<SiteIdentifierDto> Identifiers { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the PartyRoleSite record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}
