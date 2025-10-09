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


    public static AddressDocument FromDomain(Address m) => new()
    {
        IdentifierId = m.Id,
        Uprn = m.Uprn,
        AddressLine1 = m.AddressLine1,
        AddressLine2 = m.AddressLine2,
        PostTown = m.PostTown,
        County = m.County,
        PostCode = m.PostCode,
        Country = m.Country is not null ? CountryDocument.FromDomain(m.Country) : null,
        LastUpdatedDate = m.LastUpdatedDate
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