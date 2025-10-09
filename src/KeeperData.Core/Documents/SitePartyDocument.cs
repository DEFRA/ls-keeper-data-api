using KeeperData.Core.Domain.BuildingBlocks;
using KeeperData.Core.Domain.Sites;
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
}