using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// The list of roles wrapped with a count of available records.
/// </summary>
public class RoleListResponse
{
    /// <summary>
    /// Total number of roles returned (or available).
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// The list of role details.
    /// </summary>
    [JsonPropertyName("values")]
    public List<RoleDto> Values { get; set; } = [];
}