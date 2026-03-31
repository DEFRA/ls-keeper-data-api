using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class CommunicationDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The email address.
    /// </summary>
    /// <example>john.doe@somecompany.co.uk</example>
    [BsonElement("email")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// The mobile phone number.
    /// </summary>
    /// <example>07123456789</example>
    [BsonElement("mobile")]
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// The landline phone number.
    /// </summary>
    /// <example>0114 1231234</example>
    [BsonElement("landline")]
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }

    /// <summary>
    /// Indicates whether this is the primary contact.
    /// </summary>
    /// <example>true</example>
    [BsonElement("primaryContactFlag")]
    [JsonPropertyName("primaryContactFlag")]
    public bool? PrimaryContactFlag { get; set; }

    /// <summary>
    /// The timestamp of the last time the Communication record was updated.
    /// </summary>
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