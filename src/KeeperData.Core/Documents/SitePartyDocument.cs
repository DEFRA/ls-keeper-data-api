using KeeperData.Core.Domain.Sites; // Add this using
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class SitePartyDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Name { get; set; }
    public string? CustomerNumber { get; set; }
    public string? PartyType { get; set; }
    public List<CommunicationDocument> Communication { get; set; } = [];
    public AddressDocument? CorrespondanceAddress { get; set; }
    public List<RolesToPartyDocument> PartyRoles { get; set; } = [];
    public string? State { get; set; }
    public DateTime? LastUpdatedDate { get; set; }

    public static SitePartyDocument FromDomain(Party m) => new()
    {
        IdentifierId = m.Id,
        Title = m.Title,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Name = m.Name,
        CustomerNumber = m.CustomerNumber,
        PartyType = m.PartyType,
        Communication = [.. m.Communication.Select(CommunicationDocument.FromDomain)],
        CorrespondanceAddress = m.CorrespondanceAddress is not null ? AddressDocument.FromDomain(m.CorrespondanceAddress) : null,
        PartyRoles = [.. m.PartyRoles.Select(RolesToPartyDocument.FromDomain)],
        State = m.State,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public Party ToDomain() => new(
        IdentifierId,
        Title,
        FirstName,
        LastName,
        Name,
        CustomerNumber,
        PartyType,
        Communication.Select(c => c.ToDomain()),
        CorrespondanceAddress?.ToDomain(),
        PartyRoles.Select(r => r.ToDomain()),
        State,
        LastUpdatedDate
    );
}