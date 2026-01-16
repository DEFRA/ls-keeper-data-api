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

    [BsonElement("code")]
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [BsonElement("name")]
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [BsonElement("longName")]
    [JsonPropertyName("longName")]
    public string? LongName { get; set; }

    [BsonElement("euTradeMemberFlag")]
    [JsonPropertyName("euTradeMemberFlag")]
    public bool EuTradeMemberFlag { get; set; }

    [BsonElement("devolvedAuthorityFlag")]
    [JsonPropertyName("devolvedAuthorityFlag")]
    public bool DevolvedAuthorityFlag { get; set; }

    [BsonElement("lastModifiedDate")]
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