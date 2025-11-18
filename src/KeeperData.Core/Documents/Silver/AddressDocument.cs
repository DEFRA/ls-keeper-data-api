using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class AddressDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    [JsonPropertyName("addressLine")]
    [BsonElement("addressLine")]
    public string? AddressLine { get; set; }

    [JsonPropertyName("addressLocality")]
    [BsonElement("addressLocality")]
    public string? AddressLocality { get; set; }

    [JsonPropertyName("addressStreet")]
    [BsonElement("addressStreet")]
    public string? AddressStreet { get; set; }

    [JsonPropertyName("addressTown")]
    [BsonElement("addressTown")]
    public string? AddressTown { get; set; }

    [JsonPropertyName("addressPostCode")]
    [BsonElement("addressPostCode")]
    public string? AddressPostCode { get; set; }

    [JsonPropertyName("countrySubDivision")]
    [BsonElement("countrySubDivision")]
    public string? CountrySubDivision { get; set; }

    [JsonPropertyName("countryIdentifier")]
    [BsonElement("countryIdentifier")]
    public string? CountryIdentifier { get; set; } // LOV Lookup / Internal Id

    [JsonPropertyName("countryCode")]
    [BsonElement("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("uniquePropertyReferenceNumber")]
    [BsonElement("uniquePropertyReferenceNumber")]
    public string? UniquePropertyReferenceNumber { get; set; }
}