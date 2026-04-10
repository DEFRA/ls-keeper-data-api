using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// The geographic location of a site.
/// </summary>
public class LocationDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The Ordnance Survey map reference for the location.
    /// </summary>
    /// <example>SK25979936</example>
    [JsonPropertyName("osMapReference")]
    public string? OsMapReference { get; set; }

    /// <summary>
    /// The easting coordinate.
    /// </summary>
    /// <example>425978.12</example>
    [JsonPropertyName("easting")]
    public double? Easting { get; set; }

    /// <summary>
    /// The northing coordinate.
    /// </summary>
    /// <example>399361.50</example>
    [JsonPropertyName("northing")]
    public double? Northing { get; set; }

    /// <summary>
    /// The address of the location.
    /// </summary>
    [JsonPropertyName("address")]
    public AddressDto? Address { get; set; }

    /// <summary>
    /// The communication details for the location.
    /// </summary>
    [JsonPropertyName("communication")]
    public List<CommunicationDto> Communication { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the Location record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}