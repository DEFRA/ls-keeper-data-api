using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteActivityDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("type")]
    [JsonPropertyName("type")]
    public required PremisesActivityTypeSummaryDocument Type { get; set; }

    [BsonElement("startDate")]
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [BsonElement("endDate")]
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    public static SiteActivityDocument FromDomain(SiteActivity m) => new()
    {
        IdentifierId = m.Id,
        Type = PremisesActivityTypeSummaryDocument.FromDomain(m.Type),
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public SiteActivity ToDomain() => new(
        IdentifierId,
        Type.ToDomain(),
        StartDate ?? DateTime.MinValue,
        EndDate,
        LastUpdatedDate
    );
}