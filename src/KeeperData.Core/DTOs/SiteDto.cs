using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A site (premises/holding) returned by the Sites API.
/// </summary>
public class SiteDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The timestamp of the last time the Site record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// The type of site (e.g. Agricultural Holding, Market).
    /// </summary>
    [JsonPropertyName("type")]
    public SiteTypeSummaryDto? Type { get; set; }

    /// <summary>
    /// The name of the site.
    /// </summary>
    /// <example>Hansel &amp; Gretel Farm</example>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// The current state of the site.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// The date the site was established.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The date the site was decommissioned, if applicable.
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The source system of the data (e.g. SAM, CTS).
    /// </summary>
    /// <example>SAM</example>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Indicates whether identity documents should be destroyed for this site.
    /// </summary>
    [JsonPropertyName("destroyIdentityDocumentsFlag")]
    public bool? DestroyIdentityDocumentsFlag { get; set; }

    /// <summary>
    /// The geographic location of the site.
    /// </summary>
    [JsonPropertyName("location")]
    public LocationDto? Location { get; set; }

    /// <summary>
    /// The identifiers associated with this site (e.g. CPH Number).
    /// </summary>
    [JsonPropertyName("identifiers")]
    public List<SiteIdentifierDto> Identifiers { get; set; } = [];

    /// <summary>
    /// The parties associated with this site.
    /// </summary>
    [JsonPropertyName("parties")]
    public List<SitePartyDto> Parties { get; set; } = [];

    /// <summary>
    /// The species registered at this site.
    /// </summary>
    [JsonPropertyName("species")]
    public List<SpeciesSummaryDto> Species { get; set; } = [];

    /// <summary>
    /// The herd/flock/group marks associated with this site.
    /// </summary>
    [JsonPropertyName("marks")]
    public List<GroupMarkDto> Marks { get; set; } = [];

    /// <summary>
    /// The activities associated with this site.
    /// </summary>
    [JsonPropertyName("activities")]
    public List<SiteActivityDto> Activities { get; set; } = [];
}