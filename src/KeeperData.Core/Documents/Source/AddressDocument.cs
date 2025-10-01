using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Source;

public class AddressDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? AddressLine { get; set; }
    public string? AddressLocality { get; set; }
    public string? AddressPostCode { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressTown { get; set; }
    public string? CountryIdentifier { get; set; }
    public string? UniquePropertyReferenceNumber { get; set; }
}