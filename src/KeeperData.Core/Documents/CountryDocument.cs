using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class CountryDocument : INestedEntity
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

    [BsonElement("isActive")]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [BsonElement("euTradeMember")]
    [JsonPropertyName("euTradeMember")]
    public bool EuTradeMember { get; set; }

    [BsonElement("devolvedAuthority")]
    [JsonPropertyName("devolvedAuthority")]
    public bool DevolvedAuthority { get; set; }

    [BsonElement("sortOrder")]
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("effectiveStartDate")]
    [JsonPropertyName("effectiveStartDate")]
    public DateTime EffectiveStartDate { get; set; }

    [BsonElement("effectiveEndDate")]
    [JsonPropertyName("effectiveEndDate")]
    public DateTime? EffectiveEndDate { get; set; }

    [BsonElement("createdBy")]
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [BsonElement("lastModifiedBy")]
    [JsonPropertyName("lastModifiedBy")]
    public string? LastModifiedBy { get; set; }

    [BsonElement("lastModifiedDate")]
    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }

    public static CountryDocument FromDomain(Country country) => new()
    {
        IdentifierId = country.Id,
        Code = country.Code,
        Name = country.Name,
        LongName = country.LongName,
        EuTradeMember = country.EuTradeMemberFlag,
        DevolvedAuthority = country.DevolvedAuthorityFlag,
        LastModifiedDate = country.LastUpdatedDate
    };

    public Country ToDomain() => new(
        IdentifierId,
        Code,
        Name,
        LongName,
        EuTradeMember,
        DevolvedAuthority,
        LastModifiedDate
    );
}