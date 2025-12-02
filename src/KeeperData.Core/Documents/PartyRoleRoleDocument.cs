using KeeperData.Core.Domain.Shared;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PartyRoleRoleDocument
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static PartyRoleRoleDocument FromDomain(PartyRoleRole m) => new()
    {
        IdentifierId = m.Id,
        Name = m.Name,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public PartyRoleRole ToDomain()
    {
        return new PartyRoleRole(
            IdentifierId,
            Name,
            LastUpdatedDate
        );
    }
}