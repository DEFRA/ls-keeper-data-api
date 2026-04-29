using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A species reference record.
/// </summary>
public class SpeciesDTO
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The species code.
    /// </summary>
    /// <example>CTT</example>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The species name.
    /// </summary>
    /// <example>Cattle</example>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The timestamp of the last time the Species record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}