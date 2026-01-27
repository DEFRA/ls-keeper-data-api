using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class GroupMarkDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("mark")]
    [JsonPropertyName("mark")]
    public required string Mark { get; set; }

    [BsonElement("startDate")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [BsonElement("species")]
    [JsonPropertyName("species")]
    public List<SpeciesSummaryDocument> Species { get; set; } = [];

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    public static GroupMarkDocument FromDomain(GroupMark m) => new()
    {
        IdentifierId = m.Id,
        Mark = m.Mark,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        Species = m.Species.Select(SpeciesSummaryDocument.FromDomain).ToList(),
        LastUpdatedDate = m.LastUpdatedDate
    };

    public GroupMark ToDomain() => new(
        IdentifierId,
        LastUpdatedDate,
        Mark,
        StartDate,
        EndDate,
        Species.Select(s => s.ToDomain()));
}