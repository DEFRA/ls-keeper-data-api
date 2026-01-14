using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

/// <summary>
/// Composite key: PartyId
/// </summary>
[CollectionName("samParties")]
public class SamPartyDocument : BasePartyDocument, IEntity, IDeletableEntity, IContainsIndexes
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

    /// <summary>
    /// Set only if source is SAM Holder
    /// </summary>
    [JsonPropertyName("countyParishHoldingNumber")]
    [BsonElement("countyParishHoldingNumber")]
    [AutoIndexed]
    public string? CountyParishHoldingNumber { get; set; }

    [JsonPropertyName("cphList")]
    [BsonElement("cphList")]
    public List<string> CphList { get; set; } = [];

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexedAttribute.GetIndexModels<SamPartyDocument>();
    }
}