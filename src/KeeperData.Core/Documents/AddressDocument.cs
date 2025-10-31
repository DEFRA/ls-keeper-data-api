using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class AddressDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }

    public int? Uprn { get; set; }
    public required string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? PostTown { get; set; }
    public string? County { get; set; }
    public required string PostCode { get; set; }
    public CountryDocument? Country { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    public static AddressDocument FromDomain(Address address) => new()
    {
        IdentifierId = address.Id,
        Uprn = address.Uprn,
        AddressLine1 = address.AddressLine1,
        AddressLine2 = address.AddressLine2,
        PostTown = address.PostTown,
        County = address.County,
        PostCode = address.PostCode,
        Country = address.Country is not null ? CountryDocument.FromDomain(address.Country) : null,
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