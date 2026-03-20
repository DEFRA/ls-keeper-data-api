using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// The type of site an animal may reside at.
/// </summary>
public class PremisesTypeSummaryDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string IdentifierId { get; set; } = string.Empty;

    /// <summary>
    /// The business key/code values for a siteType.
    /// </summary>
    /// <example>AH</example>
    [BsonElement("code")]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The type of site an animal may reside at.
    /// </summary>
    /// <example>Agricultural Holding</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the last time the SiteType record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static PremisesTypeSummaryDocument FromDomain(PremisesType domain) => new()
    {
        IdentifierId = domain.Id,
        Code = domain.Code,
        Name = domain.Description,
        LastUpdatedDate = domain.LastUpdatedDate
    };

    public PremisesType ToDomain() => new(
        IdentifierId,
        Code,
        Name,
        LastUpdatedDate
    );
}