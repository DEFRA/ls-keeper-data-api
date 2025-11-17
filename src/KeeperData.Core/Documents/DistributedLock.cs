using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("distributed_locks")]
public class DistributedLock : IEntity
{
    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [BsonElement("owner")]
    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty;

    [BsonElement("expiresAtUtc")]
    [JsonPropertyName("expiresAtUtc")]
    public DateTimeOffset ExpiresAtUtc { get; set; }
}