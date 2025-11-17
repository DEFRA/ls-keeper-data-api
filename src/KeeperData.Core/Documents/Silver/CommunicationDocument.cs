using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class CommunicationDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    [JsonPropertyName("email")]
    [BsonElement("email")]
    public string? Email { get; set; }

    [JsonPropertyName("mobile")]
    [BsonElement("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("landline")]
    [BsonElement("landline")]
    public string? Landline { get; set; }
}