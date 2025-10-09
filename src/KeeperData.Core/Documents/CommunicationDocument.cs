using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class CommunicationDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; } 

    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Landline { get; set; }
    public bool? PrimaryContactFlag { get; set; } 
    public DateTime? LastUpdatedDate { get; set; }

    public static CommunicationDocument FromDomain(Communication m) => new()
    {
        IdentifierId = m.Id,
        Email = m.Email,
        Mobile = m.Mobile,
        Landline = m.Landline,
        PrimaryContactFlag = m.PrimaryContactFlag,
        LastUpdatedDate = m.LastUpdatedDate,
    };

    public Communication ToDomain() => new(
        IdentifierId,
        Email,
        Mobile,
        Landline,
        PrimaryContactFlag,
        LastUpdatedDate);
}
