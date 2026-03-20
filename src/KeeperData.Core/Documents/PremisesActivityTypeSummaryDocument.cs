using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// The type of activity associated with a site.
/// </summary>
public class PremisesActivityTypeSummaryDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The activity type code.
    /// </summary>
    /// <example>WP</example>
    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The activity type name.
    /// </summary>
    /// <example>Wildlife Park</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// The timestamp of the last time the SiteActivityType record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static PremisesActivityTypeSummaryDocument FromDomain(SiteActivityType m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code,
        Name = m.Name,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public SiteActivityType ToDomain() => new(
        id: IdentifierId,
        code: Code,
        name: Name,
        lastUpdatedDate: LastUpdatedDate);
}