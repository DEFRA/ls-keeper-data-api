using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SitePartyDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("partyId")]
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    [BsonElement("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [BsonElement("firstName")]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [BsonElement("lastName")]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [BsonElement("partyType")]
    [JsonPropertyName("partyType")]
    public string? PartyType { get; set; }

    [BsonElement("state")]
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [BsonElement("communication")]
    [JsonPropertyName("communication")]
    public List<CommunicationDocument> Communication { get; set; } = [];

    [BsonElement("correspondanceAddress")]
    [JsonPropertyName("correspondanceAddress")]
    public AddressDocument? CorrespondanceAddress { get; set; }

    [BsonElement("partyRoles")]
    [JsonPropertyName("partyRoles")]
    public List<PartyRoleDocument> PartyRoles { get; set; } = [];

    public static SitePartyDocument FromDomain(SiteParty m) => new()
    {
        IdentifierId = m.Id,
        PartyId = m.PartyId,
        Title = m.Title,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Name = m.Name,
        PartyType = m.PartyType,
        Communication = [.. m.Communication.Select(CommunicationDocument.FromDomain)],
        CorrespondanceAddress = m.CorrespondanceAddress is not null ? AddressDocument.FromDomain(m.CorrespondanceAddress) : null,
        PartyRoles = [.. m.PartyRoles.Select(PartyRoleDocument.FromDomain)],
        State = m.State,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public SiteParty ToDomain() => new(
        IdentifierId,
        LastUpdatedDate,
        PartyId,
        Title,
        FirstName,
        LastName,
        Name,
        PartyType,
        State,
        CorrespondanceAddress?.ToDomain(),
        Communication.Select(c => c.ToDomain()),
        PartyRoles.Select(r => r.ToDomain())
    );
}