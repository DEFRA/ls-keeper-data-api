using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class AddressDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The Unique Property Reference Number.
    /// </summary>
    /// <example>671544009</example>
    [BsonElement("uprn")]
    [BsonRepresentation(BsonType.String)]
    [JsonPropertyName("uprn")]
    public string? Uprn { get; set; }

    /// <summary>
    /// This single address line is associated with the OS Address Base Fields such as SAO_TEXT, SAO_START_NUMBER, PAO_TEXT, PAO_START_NUMBER and STREET_DESCRIPTION.
    /// </summary>
    /// <example>Hansel &amp; Gretel Farm, Pigs Street</example>
    [BsonElement("addressLine1")]
    [JsonPropertyName("addressLine1")]
    public required string AddressLine1 { get; set; }

    /// <summary>
    /// This optional field is associated with the OS Address Base Field of LOCALITY, if applicable.
    /// </summary>
    /// <example>Cloverfield</example>
    [BsonElement("addressLine2")]
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// This field describes the Town or City of the Address. It is aligned to the OS Address Base Field of POST_TOWN.
    /// </summary>
    /// <example>Clover town</example>
    [BsonElement("postTown")]
    [JsonPropertyName("postTown")]
    public string? PostTown { get; set; }

    /// <summary>
    /// This optional field is associated with the OS Address Base Field of ADMINISTRATIVE_AREA.
    /// </summary>
    /// <example>Sussex</example>
    [BsonElement("county")]
    [JsonPropertyName("county")]
    public string? County { get; set; }

    /// <summary>
    /// The postal code.
    /// </summary>
    /// <example>S36 2BS</example>
    [BsonElement("postcode")]
    [JsonPropertyName("postcode")]
    public required string Postcode { get; set; }

    /// <summary>
    /// The country associated with the address.
    /// </summary>
    [BsonElement("country")]
    [JsonPropertyName("country")]
    public CountrySummaryDocument? Country { get; set; }

    /// <summary>
    /// The timestamp of the last time the Address record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    public static AddressDocument FromDomain(Address address) => new()
    {
        IdentifierId = address.Id,
        Uprn = address.Uprn,
        AddressLine1 = address.AddressLine1,
        AddressLine2 = address.AddressLine2,
        PostTown = address.PostTown,
        County = address.County,
        Postcode = address.PostCode,
        Country = address.Country is not null ? CountrySummaryDocument.FromDomain(address.Country) : null,
        LastUpdatedDate = address.LastUpdatedDate
    };

    public Address ToDomain() => new(
        IdentifierId,
        Uprn,
        AddressLine1,
        AddressLine2,
        PostTown,
        County,
        Postcode,
        Country?.ToDomain(),
        LastUpdatedDate);
}