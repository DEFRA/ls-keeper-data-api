using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PremisesTypeSummaryDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string IdentifierId { get; set; } = string.Empty;

    [BsonElement("code")]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("description")]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

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