using KeeperData.Core.Attributes;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Reference;

[CollectionName("referenceData")]
public class ProductionUsageListDocument : IListDocument, IReferenceListDocument<ProductionUsageDocument>
{
    public static string DocumentId => "all-productionusages";

    [BsonId]
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = DocumentId;

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("productionUsages")]
    [JsonPropertyName("productionUsages")]
    public List<ProductionUsageDocument> ProductionUsages { get; set; } = [];

    public IReadOnlyCollection<ProductionUsageDocument> Items => ProductionUsages.AsReadOnly();
}