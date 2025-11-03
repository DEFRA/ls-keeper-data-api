using KeeperData.Core.Domain.Sites;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PartyRoleDocument : INestedEntity
{
    [JsonPropertyName("id")]
    [BsonElement("id")]
    public required string IdentifierId { get; set; }
    public string? Role { get; set; }
    public List<ManagedSpeciesDocument> SpeciesManagedByRole { get; set; } = [];
    public DateTime? LastUpdatedDate { get; set; }

    public static PartyRoleDocument FromDomain(PartyRole m) => new()
    {
        IdentifierId = m.Id,
        Role = m.Role?.Name,
        SpeciesManagedByRole = [.. m.SpeciesManagedByRole.Select(ManagedSpeciesDocument.FromDomain)],
        LastUpdatedDate = m.LastUpdatedDate
    };

    public PartyRole ToDomain()
    {
        var roleObject = Role is not null
            ? new Role(Guid.NewGuid().ToString(), Role, LastUpdatedDate)
            : null;

        return new PartyRole(
            IdentifierId,
            roleObject,
            SpeciesManagedByRole.Select(s => s.ToDomain()),
            LastUpdatedDate
        );
    }
}