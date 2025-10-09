using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SpeciesDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}