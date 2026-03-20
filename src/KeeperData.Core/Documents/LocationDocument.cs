using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class LocationDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The Ordnance Survey map reference for the location.
    /// </summary>
    /// <example>SK25979936</example>
    [BsonElement("osMapReference")]
    [JsonPropertyName("osMapReference")]
    public string? OsMapReference { get; set; }

    /// <summary>
    /// The easting coordinate.
    /// </summary>
    /// <example>425978.12</example>
    [BsonElement("easting")]
    [JsonPropertyName("easting")]
    public double? Easting { get; set; }

    /// <summary>
    /// The northing coordinate.
    /// </summary>
    /// <example>399361.50</example>
    [BsonElement("northing")]
    [JsonPropertyName("northing")]
    public double? Northing { get; set; }

    /// <summary>
    /// The address of the location.
    /// </summary>
    [BsonElement("address")]
    [JsonPropertyName("address")]
    public AddressDocument? Address { get; set; }

    /// <summary>
    /// The communication details for the location.
    /// </summary>
    [BsonElement("communication")]
    [JsonPropertyName("communication")]
    public List<CommunicationDocument> Communication { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the Location record was updated.
    /// </summary>
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