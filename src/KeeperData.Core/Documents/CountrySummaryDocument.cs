using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class CountrySummaryDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("longName")]
    public string? LongName { get; set; }

    [JsonPropertyName("euTradeMemberFlag")]
    public bool EuTradeMemberFlag { get; set; }

    [JsonPropertyName("devolvedAuthorityFlag")]
    public bool DevolvedAuthorityFlag { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }

    public static CountrySummaryDocument FromDomain(Country country) => new()
    {
        IdentifierId = country.Id,
        Code = country.Code,
        Name = country.Name,
        LongName = country.LongName,
        EuTradeMemberFlag = country.EuTradeMemberFlag,
        DevolvedAuthorityFlag = country.DevolvedAuthorityFlag,
        LastModifiedDate = country.LastUpdatedDate
    };

    public Country ToDomain() => new(
        IdentifierId,
        Code,
        Name,
        LongName,
        EuTradeMemberFlag,
        DevolvedAuthorityFlag,
        LastModifiedDate
    );
}