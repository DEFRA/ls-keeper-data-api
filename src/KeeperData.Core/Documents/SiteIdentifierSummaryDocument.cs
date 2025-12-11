using KeeperData.Core.Domain.Sites;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteIdentifierSummaryDocument
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [BsonElement("description")]
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static SiteIdentifierSummaryDocument FromDomain(SiteIdentifierType m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code ?? string.Empty,
        Description = m.Name ?? string.Empty,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public SiteIdentifierType ToDomain() => new(
        id: IdentifierId,
        code: Code,
        name: Description,
        lastUpdatedDate: LastUpdatedDate);
}
