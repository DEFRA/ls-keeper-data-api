using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("refProductionUsages")]
public class ProductionUsageListDocument : IListDocument
{
    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = "all-productionusages";

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("productionUsages")]
    [JsonPropertyName("productionUsages")]
    public List<ProductionUsageDocument> ProductionUsages { get; set; } = [];
}