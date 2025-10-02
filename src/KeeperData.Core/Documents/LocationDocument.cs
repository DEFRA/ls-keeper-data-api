using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class LocationDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public string? OsMapReference { get; set; }
    public double? Easting { get; set; }
    public double? Northing { get; set; }

    public static LocationDocument FromDomain(Location m) => new()
    {
        IdentifierId = m.Id,
        LastUpdatedDate = m.LastUpdatedDate,
        OsMapReference = m.OsMapReference,
        Easting = m.Easting,
        Northing = m.Northing,
    };

    public Location ToDomain() => new(
        IdentifierId,
        LastUpdatedDate,
        OsMapReference,
        Easting,
        Northing);
}