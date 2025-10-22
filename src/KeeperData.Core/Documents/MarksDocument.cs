using KeeperData.Core.Domain.Sites; // Add this using
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class MarksDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public required string Mark { get; set; }
    public SpeciesDocument? Species { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public static MarksDocument FromDomain(Marks m) => new()
    {
        IdentifierId = m.Id,
        Mark = m.Mark,
        Species = m.Species is not null ? SpeciesDocument.FromDomain(m.Species) : null,
        StartDate = m.StartDate,
        EndDate = m.EndDate
    };

    public Marks ToDomain() => new(
        IdentifierId,
        Mark,
        Species?.ToDomain(),
        StartDate,
        EndDate
    );
}