using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class AddressDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [JsonPropertyName("uprn")]
    public int? Uprn { get; set; }

    [JsonPropertyName("addressLine1")]
    public required string AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("postTown")]
    public string? PostTown { get; set; }

    [JsonPropertyName("county")]
    public string? County { get; set; }

    [JsonPropertyName("postCode")]
    public required string PostCode { get; set; }

    [JsonPropertyName("country")]
    public CountrySummaryDocument? Country { get; set; }

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
        PostCode = address.PostCode,
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
        PostCode,
        Country?.ToDomain(),
        LastUpdatedDate);
}