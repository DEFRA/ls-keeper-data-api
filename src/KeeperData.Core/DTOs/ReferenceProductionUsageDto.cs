using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// An isolated production usage reference record.
/// </summary>
public class ReferenceProductionUsageDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The production usage code.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The production usage description.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }
}