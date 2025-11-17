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

    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("partyType")]
    public string? PartyType { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    [JsonPropertyName("communication")]
    public List<CommunicationDocument> Communication { get; set; } = [];

    [JsonPropertyName("correspondanceAddress")]
    public AddressDocument? CorrespondanceAddress { get; set; }

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