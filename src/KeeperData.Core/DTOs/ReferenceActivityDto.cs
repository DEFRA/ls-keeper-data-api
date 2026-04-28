using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// An isolated activity (site activity type) reference record.
/// </summary>
public class ReferenceActivityDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The activity code.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The activity name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}