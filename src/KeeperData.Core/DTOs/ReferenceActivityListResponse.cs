using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// The list of reference activities wrapped with a count of available records.
/// </summary>
public class ReferenceActivityListResponse
{
    /// <summary>
    /// Total number of activities returned (or available).
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// The list of activity details.
    /// </summary>[JsonPropertyName("values")]
    public List<ReferenceActivityDto> Values { get; set; } = [];
}