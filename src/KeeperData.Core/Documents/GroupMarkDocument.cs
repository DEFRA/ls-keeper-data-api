using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class GroupMarkDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The herd/flock/group mark identifier.
    /// </summary>
    /// <example>564545</example>
    [BsonElement("mark")]
    [JsonPropertyName("mark")]
    public required string Mark { get; set; }

    /// <summary>
    /// The date the herd/flock/group mark was assigned.
    /// </summary>
    [BsonElement("startDate")]
    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// The date the herd/flock/group mark was removed.
    /// </summary>
    [BsonElement("endDate")]
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// The species associated with this mark.
    /// </summary>
    [BsonElement("species")]
    [JsonPropertyName("species")]
    public List<SpeciesSummaryDocument> Species { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the GroupMark record was updated.
    /// </summary>
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