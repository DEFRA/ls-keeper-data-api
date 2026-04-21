using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// The list of reference site types wrapped with a count of available records.
/// </summary>
public class ReferenceSiteTypeListResponse
{
    /// <summary>
    /// Total number of site types returned (or available).
    /// </summary>[JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// The list of site type details.
    /// </summary>
    [JsonPropertyName("values")]
    public List<ReferenceSiteTypeDto> Values { get; set; } = [];
}