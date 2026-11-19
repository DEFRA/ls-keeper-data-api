using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// A lightweight country summary embedded within address records.
/// </summary>
public class CountrySummaryDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The country code.
    /// </summary>
    /// <example>GB-ENG</example>
    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The country name.
    /// </summary>
    /// <example>England</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The long name of the country.
    /// </summary>
    /// <example>England - United Kingdom</example>
    [BsonElement("longName")]
    [JsonPropertyName("longName")]
    public string? LongName { get; set; }

    /// <summary>
    /// Indicates whether the country is an EU trade member.
    /// </summary>
    [BsonElement("euTradeMemberFlag")]
    [JsonPropertyName("euTradeMemberFlag")]
    public bool EuTradeMemberFlag { get; set; }

    /// <summary>
    /// Indicates whether the country is a devolved authority.
    /// </summary>
    [BsonElement("devolvedAuthorityFlag")]
    [JsonPropertyName("devolvedAuthorityFlag")]
    public bool DevolvedAuthorityFlag { get; set; }

    /// <summary>
    /// The timestamp of the last time the Country record was updated.
    /// </summary>
    [BsonElement("lastModifiedDate")]
    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }
}