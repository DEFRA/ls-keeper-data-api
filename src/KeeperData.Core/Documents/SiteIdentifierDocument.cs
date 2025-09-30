using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteIdentifierDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public string Identifier { get; set; } = default!;
    public string Type { get; set; } = default!;

    public static SiteIdentifierDocument FromDomain(SiteIdentifier si) => new()
    {
        IdentifierId = si.Id,
        LastUpdatedDate = si.LastUpdatedDate,
        Identifier = si.Identifier,
        Type = si.Type,
    };

    public SiteIdentifier ToDomain() => new(
        IdentifierId,
        LastUpdatedDate,
        Identifier,
        Type);
}