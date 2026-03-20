using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PartyRoleRoleDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// This is the unique code assigned to the Role entity.
    /// </summary>
    /// <example>LIVESTOCKKEEPER</example>
    [BsonElement("code")]
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// The name of the role.
    /// </summary>
    /// <example>Livestock Keeper</example>
    [BsonElement("name")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The timestamp of the last time the Role record was updated.
    /// </summary>
    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static PartyRoleRoleDocument FromDomain(PartyRoleRole m) => new()
    {
        IdentifierId = m.Id,
        Code = m.Code,
        Name = m.Name,
        LastUpdatedDate = m.LastUpdatedDate
    };

    public PartyRoleRole ToDomain()
    {
        return new PartyRoleRole(
            IdentifierId,
            Code,
            Name,
            LastUpdatedDate
        );
    }
}