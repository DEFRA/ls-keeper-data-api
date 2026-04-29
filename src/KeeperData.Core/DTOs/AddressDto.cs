using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// An address associated with a site location or party.
/// </summary>
public class AddressDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The Unique Property Reference Number.
    /// </summary>
    /// <example>671544009</example>
    [JsonPropertyName("uprn")]
    public string? Uprn { get; set; }

    /// <summary>
    /// This single address line is associated with the OS Address Base Fields such as SAO_TEXT, SAO_START_NUMBER, PAO_TEXT, PAO_START_NUMBER and STREET_DESCRIPTION.
    /// </summary>
    /// <example>Hansel &amp; Gretel Farm, Pigs Street</example>
    [JsonPropertyName("addressLine1")]
    public required string AddressLine1 { get; set; }

    /// <summary>
    /// This optional field is associated with the OS Address Base Field of LOCALITY, if applicable.
    /// </summary>
    /// <example>Cloverfield</example>
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// This field describes the Town or City of the Address. It is aligned to the OS Address Base Field of POST_TOWN.
    /// </summary>
    /// <example>Clover town</example>
    [JsonPropertyName("postTown")]
    public string? PostTown { get; set; }

    /// <summary>
    /// This optional field is associated with the OS Address Base Field of ADMINISTRATIVE_AREA.
    /// </summary>
    /// <example>Sussex</example>
    [JsonPropertyName("county")]
    public string? County { get; set; }

    /// <summary>
    /// The postal code.
    /// </summary>
    /// <example>S36 2BS</example>
    [JsonPropertyName("postcode")]
    public required string Postcode { get; set; }

    /// <summary>
    /// The country associated with the address.
    /// </summary>
    [JsonPropertyName("country")]
    public CountrySummaryDto? Country { get; set; }

    /// <summary>
    /// The timestamp of the last time the Address record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}