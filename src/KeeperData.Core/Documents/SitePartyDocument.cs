using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// A party associated with a site, containing their contact details, address and roles.
/// </summary>
public class SitePartyDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// This is the L or the C number provided from the SAM system.
    /// </summary>
    /// <example>C77473</example>
    [BsonElement("customerNumber")]
    [JsonPropertyName("customerNumber")]
    public string CustomerNumber { get; set; } = string.Empty;

    /// <summary>
    /// The title of the party (e.g. Mr, Mrs).
    /// </summary>
    /// <example>Mr</example>
    [BsonElement("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The first name of the party.
    /// </summary>
    /// <example>John</example>
    [BsonElement("firstName")]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// The last name of the party.
    /// </summary>
    /// <example>Doe</example>
    [BsonElement("lastName")]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// The full name of the party.
    /// </summary>
    /// <example>John Doe</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The type of party (e.g. Person, Organisation).
    /// </summary>
    /// <example>Person</example>
    [BsonElement("partyType")]
    [JsonPropertyName("partyType")]
    public string? PartyType { get; set; }

    /// <summary>
    /// The current state of the party.
    /// </summary>
    [BsonElement("state")]
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [BsonElement("createdDate")]
    [JsonPropertyName("createdDate")]
    [JsonIgnore]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The timestamp of the last time the Site Party record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// The communication details of the party.
    /// </summary>
    [BsonElement("communication")]
    [JsonPropertyName("communication")]
    public List<CommunicationDocument> Communication { get; set; } = [];

    /// <summary>
    /// The correspondence address of the party.
    /// </summary>
    [BsonElement("correspondanceAddress")]
    [JsonPropertyName("correspondanceAddress")]
    public AddressDocument? CorrespondanceAddress { get; set; }

    /// <summary>
    /// The roles assigned to the party.
    /// </summary>
    [BsonElement("partyRoles")]
    [JsonPropertyName("partyRoles")]
    public List<PartyRoleDocument> PartyRoles { get; set; } = [];
}