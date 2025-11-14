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

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("landline")]
    public string? Landline { get; set; }

    [JsonPropertyName("primaryContactFlag")]
    public bool? PrimaryContactFlag { get; set; }

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