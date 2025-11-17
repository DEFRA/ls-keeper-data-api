using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class LocationDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    [JsonPropertyName("easting")]
    [BsonElement("easting")]
    public double? Easting { get; set; }

    [JsonPropertyName("northing")]
    [BsonElement("northing")]
    public double? Northing { get; set; }

    [JsonPropertyName("osMapReference")]
    [BsonElement("osMapReference")]
    public string? OsMapReference { get; set; }

    [JsonPropertyName("address")]
    [BsonElement("address")]
    public AddressDocument? Address { get; set; }
}