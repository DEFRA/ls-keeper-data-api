using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

[CollectionName("ports")]
public class PortDocument : IEntity, IDeletableEntity, IContainsIndexes
{
    [BsonId]
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public string? Id { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    [AutoIndexed]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("deleted")]
    [JsonPropertyName("deleted")]
    [AutoIndexed]
    public bool Deleted { get; set; }

    [BsonElement("changeType")]
    [JsonPropertyName("changeType")]
    public string ChangeType { get; set; } = string.Empty;

    [BsonElement("holdingIdentifier")]
    [JsonPropertyName("holdingIdentifier")]
    [AutoIndexed]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [BsonElement("addressLine1")]
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1 { get; set; }

    [BsonElement("addressLine2")]
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    [BsonElement("addressLine3")]
    [JsonPropertyName("addressLine3")]
    public string? AddressLine3 { get; set; }

    [BsonElement("postcode")]
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [BsonElement("mapReference")]
    [JsonPropertyName("mapReference")]
    public string? MapReference { get; set; }

    [BsonElement("easting")]
    [JsonPropertyName("easting")]
    public int? Easting { get; set; }

    [BsonElement("northing")]
    [JsonPropertyName("northing")]
    public int? Northing { get; set; }

    [BsonElement("source")]
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
    {
        return AutoIndexedAttribute.GetIndexModels<PortDocument>();
    }
}