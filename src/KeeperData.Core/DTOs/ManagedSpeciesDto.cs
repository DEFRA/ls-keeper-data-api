using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A species managed by a party under a specific role.
/// </summary>
public class ManagedSpeciesDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The species code.
    /// </summary>
    /// <example>CTT</example>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The species name.
    /// </summary>
    /// <example>Cattle</example>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The date the said species came under the care of the said Role.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The date the said species left the care of the said Role.
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The timestamp of the last time the ManagedSpecies record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}