using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents.Silver;

public class BasePartyDocument
{
    [JsonPropertyName("partyId")]
    [BsonElement("partyId")]
    public string PartyId { get; set; } = string.Empty;

    [JsonPropertyName("partyTypeId")]
    [BsonElement("partyTypeId")]
    public string PartyTypeId { get; set; } = string.Empty;

    [JsonPropertyName("partyFullName")]
    [BsonElement("partyFullName")]
    public string? PartyFullName { get; set; }

    [JsonPropertyName("partyTitleTypeIdentifier")]
    [BsonElement("partyTitleTypeIdentifier")]
    public string? PartyTitleTypeIdentifier { get; set; }

    [JsonPropertyName("partyFirstName")]
    [BsonElement("partyFirstName")]
    public string? PartyFirstName { get; set; }

    [JsonPropertyName("partyInitials")]
    [BsonElement("partyInitials")]
    public string? PartyInitials { get; set; }

    [JsonPropertyName("partyLastName")]
    [BsonElement("partyLastName")]
    public string? PartyLastName { get; set; }

    [JsonPropertyName("address")]
    [BsonElement("address")]
    public AddressDocument? Address { get; set; }

    [JsonPropertyName("communication")]
    [BsonElement("communication")]
    public CommunicationDocument? Communication { get; set; }

    [JsonPropertyName("roles")]
    [BsonElement("roles")]
    public List<PartyRoleDocument>? Roles { get; set; } = [];
}