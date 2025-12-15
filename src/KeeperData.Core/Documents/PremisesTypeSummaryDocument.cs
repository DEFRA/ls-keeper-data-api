using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PremisesTypeSummaryDocument : INestedEntity
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

    public static PremisesTypeSummaryDocument FromDomain(PremisesType domain) => new()
    {
        IdentifierId = domain.Id,
        Code = domain.Code,
        Description = domain.Description,
        LastUpdatedDate = domain.LastUpdatedDate
    };

    public PremisesType ToDomain() => new(
        IdentifierId,
        Code,
        Description,
        LastUpdatedDate
    );
}