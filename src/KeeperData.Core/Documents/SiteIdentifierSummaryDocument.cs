using KeeperData.Core.Domain.Sites;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using KeeperData.Core.Repositories;

namespace KeeperData.Core.Documents;

/// <summary>
/// The type which identifies the site identifier.
/// </summary>
public class SiteIdentifierSummaryDocument : ISummaryDocument
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The business key/code values for a Site Identifier Type.
    /// </summary>
    /// <example>CPHN</example>
    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The type which identifies the site identifier.
    /// </summary>
    /// <example>CPH Number</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The timestamp of the last time the SiteIdentifier Type record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static SiteIdentifierSummaryDocument FromDomain(SiteIdentifierType m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code ?? string.Empty,
        Name = m.Name ?? string.Empty,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public SiteIdentifierType ToDomain() => new(
        id: IdentifierId,
        code: Code,
        name: Name,
        lastUpdatedDate: LastUpdatedDate);
}