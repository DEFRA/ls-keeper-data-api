using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A flattened site activity with type code and name promoted to top level.
/// </summary>
public class SiteActivityDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The activity type code.
    /// </summary>
    /// <example>WP</example>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The activity type name.
    /// </summary>
    /// <example>Wildlife Park</example>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The start date of the activity.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// The end date of the activity.
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }
}