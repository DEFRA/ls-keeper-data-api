using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class CommunicationDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("email")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [BsonElement("mobile")]
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [BsonElement("landline")]
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }

    [BsonElement("primaryContactFlag")]
    [JsonPropertyName("primaryContactFlag")]
    public bool? PrimaryContactFlag { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    public static CommunicationDocument FromDomain(Communication m) => new()
    {
        IdentifierId = m.Id,
        LastUpdatedDate = m.LastUpdatedDate,
        Email = m.Email,
        Mobile = m.Mobile,
        Landline = m.Landline,
        PrimaryContactFlag = m.PrimaryContactFlag
    };

    public Communication ToDomain() => new(
        IdentifierId,
        LastUpdatedDate,
        Email,
        Mobile,
        Landline,
        PrimaryContactFlag);
}