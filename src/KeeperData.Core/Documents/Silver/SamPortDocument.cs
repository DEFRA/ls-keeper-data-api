using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

/// <summary>
/// Composite key: CountyParishHoldingNumber, PremisesName
/// </summary>
[CollectionName("samPorts")]
public class SamPortDocument : IEntity, IContainsIndexes, IDeletableEntity
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
    public bool Deleted { get; set; }

    [JsonPropertyName("changeType")]
    [BsonElement("changeType")]
    public string ChangeType { get; set; } = string.Empty;

    [JsonPropertyName("countyParishHoldingNumber")]
    [BsonElement("countyParishHoldingNumber")]
    [AutoIndexed]
    public string CountyParishHoldingNumber { get; set; } = string.Empty;

    [JsonPropertyName("premisesName")]
    [BsonElement("premisesName")]
    public string? PremisesName { get; set; }

    [JsonPropertyName("addressLine1")]
    [BsonElement("addressLine1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    [BsonElement("addressLine2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("addressLine3")]
    [BsonElement("addressLine3")]
    public string? AddressLine3 { get; set; }

    [JsonPropertyName("postcode")]
    [BsonElement("postcode")]
    public string? Postcode { get; set; }

    [JsonPropertyName("mapReference")]
    [BsonElement("mapReference")]
    public string? MapReference { get; set; }

    [JsonPropertyName("easting")]
    [BsonElement("easting")]
    public int? Easting { get; set; }

    [JsonPropertyName("northing")]
    [BsonElement("northing")]
    public int? Northing { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexedAttribute.GetIndexModels<SamPortDocument>();
    }
}