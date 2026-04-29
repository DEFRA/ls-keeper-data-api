using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// The list of species wrapped with a count of available records.
/// </summary>
public class SpeciesListResponse
{
    /// <summary>
    /// Total number of site species returned (or available).
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// The list of site species details.
    /// </summary>
    [JsonPropertyName("values")]
    public List<SpeciesDTO> Values { get; set; } = [];
}