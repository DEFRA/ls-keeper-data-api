using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class LocationDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("osMapReference")]
    [JsonPropertyName("osMapReference")]
    public string? OsMapReference { get; set; }

    [BsonElement("easting")]
    [JsonPropertyName("easting")]
    public double? Easting { get; set; }

    [BsonElement("northing")]
    [JsonPropertyName("northing")]
    public double? Northing { get; set; }

    [BsonElement("address")]
    [JsonPropertyName("address")]
    public AddressDocument? Address { get; set; }

    [BsonElement("communication")]
    [JsonPropertyName("communication")]
    public List<CommunicationDocument> Communication { get; set; } = [];

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    public static LocationDocument FromDomain(Location location) => new()
    {
        IdentifierId = location.Id,
        LastUpdatedDate = location.LastUpdatedDate,
        OsMapReference = location.OsMapReference,
        Easting = location.Easting,
        Northing = location.Northing,
        Address = location.Address is not null ? AddressDocument.FromDomain(location.Address) : null,
        Communication = [.. location.Communication.Select(CommunicationDocument.FromDomain)]
    };

    public Location ToDomain() => new(
        IdentifierId,
        LastUpdatedDate,
        OsMapReference,
        Easting,
        Northing,
        Address?.ToDomain(),
        Communication.Select(c => c.ToDomain()));
}