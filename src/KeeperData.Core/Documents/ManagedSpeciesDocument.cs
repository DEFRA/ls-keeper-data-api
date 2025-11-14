using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class ManagedSpeciesDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

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