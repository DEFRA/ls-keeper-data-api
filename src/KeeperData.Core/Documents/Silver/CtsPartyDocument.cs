using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

[CollectionName("ctsParties")]
public class CtsPartyDocument : BasePartyDocument, IEntity, IDeletableEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [JsonPropertyName("lastUpdatedBatchId")]
    [BsonElement("lastUpdatedBatchId")]
    public int? LastUpdatedBatchId { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("lastUpdatedDate")]
    [BsonElement("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [JsonPropertyName("deleted")]
    [BsonElement("deleted")]
    [AutoIndexed]
    public bool Deleted { get; set; }

    [JsonPropertyName("countyParishHoldingNumber")]
    [BsonElement("countyParishHoldingNumber")]
    [AutoIndexed]
    public string CountyParishHoldingNumber { get; set; } = string.Empty;

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexedAttribute.GetIndexModels<CtsPartyDocument>();
    }
}