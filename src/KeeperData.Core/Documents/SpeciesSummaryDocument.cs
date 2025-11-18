using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SpeciesSummaryDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [BsonElement("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [BsonElement("lastModifiedDate")]
    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }

    public static SpeciesSummaryDocument FromDomain(Species m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code,
        Name = m.Name,
        LastModifiedDate = m.LastUpdatedDate
    };

    public Species ToDomain() => new(
        id: IdentifierId,
        code: Code,
        name: Name,
        lastUpdatedDate: LastModifiedDate);
}