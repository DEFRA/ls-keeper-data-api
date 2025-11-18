using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SiteIdentifierTypeDocument : INestedEntity
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
}