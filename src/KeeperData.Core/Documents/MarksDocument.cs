using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class MarksDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Mark { get; set; }
    public SpeciesDocument? Species { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}