using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// The list of reference production usages wrapped with a count of available records.
/// </summary>
public class ReferenceProductionUsageListResponse
{
    /// <summary>
    /// Total number of production usages returned (or available).
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// The list of production usage details.
    /// </summary>
    [JsonPropertyName("values")]
    public List<ReferenceProductionUsageDto> Values { get; set; } = [];
}