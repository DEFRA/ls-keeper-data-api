using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A lightweight country summary embedded within address records.
/// </summary>
public class CountrySummaryDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The country code.
    /// </summary>
    /// <example>GB-ENG</example>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The country name.
    /// </summary>
    /// <example>England</example>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The long name of the country.
    /// </summary>
    /// <example>England - United Kingdom</example>
    [JsonPropertyName("longName")]
    public string? LongName { get; set; }

    /// <summary>
    /// Indicates whether the country is an EU trade member.
    /// </summary>
    [JsonPropertyName("euTradeMemberFlag")]
    public bool EuTradeMemberFlag { get; set; }

    /// <summary>
    /// Indicates whether the country is a devolved authority.
    /// </summary>
    [JsonPropertyName("devolvedAuthorityFlag")]
    public bool DevolvedAuthorityFlag { get; set; }

    /// <summary>
    /// The timestamp of the last time the Country record was updated.
    /// </summary>
    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }
}
