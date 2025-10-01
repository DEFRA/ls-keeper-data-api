using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Source;

public class LocationDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public double? Easting { get; set; }
    public double? Northing { get; set; }
    public string? OsMapReference { get; set; }
    public AddressDocument? Address { get; set; }
}