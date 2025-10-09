using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class RolesToPartyDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Role { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<SpeciesDocument> SpeciesManagedByRole { get; set; } = [];
    public DateTime? LastUpdatedDate { get; set; }
}