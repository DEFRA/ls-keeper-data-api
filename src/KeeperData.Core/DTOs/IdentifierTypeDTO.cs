using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// An identifier type reference record.
/// </summary>
public class IdentifierTypeDTO
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The identifier type code.
    /// </summary>
    /// <example>CPHN</example>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The identifier type name.
    /// </summary>
    /// <example>CPH Number</example>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The identifier type description.
    /// </summary>
    /// <example>Description for CPH Number</example>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The timestamp of the last time the Identifier Type record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}