using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteActivityDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Activity { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
}