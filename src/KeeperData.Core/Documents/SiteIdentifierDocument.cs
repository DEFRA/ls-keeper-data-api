using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteIdentifierDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("identifier")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = default!;

    [BsonElement("type")]
    [JsonPropertyName("type")]
    public SiteIdentifierSummaryDocument Type { get; set; } = default!;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    public static SiteIdentifierDocument FromDomain(SiteIdentifier si) => new()
    {
        IdentifierId = si.Id,
        LastUpdatedDate = si.LastUpdatedDate,
        Identifier = si.Identifier,
        Type = SiteIdentifierSummaryDocument.FromDomain(si.Type),
    };

    public SiteIdentifier ToDomain() => new(
        IdentifierId,
        LastUpdatedDate,
        Identifier,
        Type.ToDomain());
}