using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SpeciesDocument : INestedEntity
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

    [BsonElement("isActive")]
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

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

    public static SpeciesDocument FromDomain(Species m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code,
        Name = m.Name,
        LastModifiedDate = m.LastUpdatedDate,

        IsActive = true,
        SortOrder = 0,
        EffectiveStartDate = new DateTime(1900, 1, 1),
        EffectiveEndDate = null,
        CreatedBy = "System_FromDomain",
        CreatedDate = DateTime.UtcNow,
        LastModifiedBy = null
    };

    public Species ToDomain() => new(
        id: IdentifierId,
        code: Code,
        name: Name,
        lastUpdatedDate: LastModifiedDate
    );
}