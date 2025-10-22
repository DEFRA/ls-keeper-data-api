using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class ManagedSpeciesDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    public static ManagedSpeciesDocument FromDomain(ManagedSpecies m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code,
        Name = m.Name,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public ManagedSpecies ToDomain() => new(
        IdentifierId,
        Code,
        Name,
        StartDate,
        EndDate,
        LastUpdatedDate
    );
}