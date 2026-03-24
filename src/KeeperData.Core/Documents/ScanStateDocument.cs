using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("scanState")]
public class ScanStateDocument : IEntity
{
    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [BsonElement("lastSuccessfulScanStartedAt")]
    [JsonPropertyName("lastSuccessfulScanStartedAt")]
    public DateTime LastSuccessfulScanStartedAt { get; set; }

    [BsonElement("lastSuccessfulScanCompletedAt")]
    [JsonPropertyName("lastSuccessfulScanCompletedAt")]
    public DateTime LastSuccessfulScanCompletedAt { get; set; }

    [BsonElement("lastScanCorrelationId")]
    [JsonPropertyName("lastScanCorrelationId")]
    public Guid LastScanCorrelationId { get; set; }

    [BsonElement("lastScanMode")]
    [JsonPropertyName("lastScanMode")]
    public string LastScanMode { get; set; } = default!;

    [BsonElement("lastScanItemCount")]
    [JsonPropertyName("lastScanItemCount")]
    public int LastScanItemCount { get; set; }
}