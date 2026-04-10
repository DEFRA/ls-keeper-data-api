using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A herd/flock/group mark associated with a site.
/// </summary>
public class GroupMarkDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The herd/flock/group mark identifier.
    /// </summary>
    /// <example>564545</example>
    [JsonPropertyName("mark")]
    public required string Mark { get; set; }

    /// <summary>
    /// The date the herd/flock/group mark was assigned.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The date the herd/flock/group mark was removed.
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The species associated with this mark.
    /// </summary>
    [JsonPropertyName("species")]
    public List<SpeciesSummaryDto> Species { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the GroupMark record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}