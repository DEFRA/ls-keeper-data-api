using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// The list of identifier types wrapped with a count of available records.
/// </summary>
public class IdentifierTypeListResponse
{
    /// <summary>
    /// Total number of identifier types returned.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// The list of identifier type details.
    /// </summary>
    [JsonPropertyName("values")]
    public List<IdentifierTypeDTO> Values { get; set; } = [];
}