using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class CountryDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }


    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("longName")]
    public string? LongName { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("euTradeMember")]
    public bool EuTradeMember { get; set; }

    [JsonPropertyName("devolvedAuthority")]
    public bool DevolvedAuthority { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("effectiveStartDate")]
    public DateTime EffectiveStartDate { get; set; }

    [JsonPropertyName("effectiveEndDate")]
    public DateTime? EffectiveEndDate { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("lastModifiedBy")]
    public string? LastModifiedBy { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }

    public static CountryDocument FromDomain(Country country) => new()
    {
        IdentifierId = country.Id,
        Code = country.Code,
        Name = country.Name,
        LongName = country.LongName,
        EuTradeMember = country.EuTradeMember,
        DevolvedAuthority = country.DevolvedAuthority,
        LastModifiedDate = country.LastUpdatedDate,

        // Persistence metadata defaults
        IsActive = true,
        SortOrder = 0,
        EffectiveStartDate = DateTime.UtcNow,
        EffectiveEndDate = null,
        CreatedBy = "System_FromDomain",
        CreatedDate = DateTime.UtcNow,
        LastModifiedBy = null
    };

    public Country ToDomain() => new(
        id: IdentifierId,
        code: Code,
        name: Name,
        longName: LongName,
        euTradeMember: EuTradeMember,
        devolvedAuthority: DevolvedAuthority,
        lastUpdatedDate: LastModifiedDate
    );
}