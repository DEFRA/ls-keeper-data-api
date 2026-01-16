using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PartyRoleDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("role")]
    [JsonPropertyName("role")]
    public required PartyRoleRoleDocument Role { get; set; }

    [BsonElement("speciesManagedByRole")]
    [JsonPropertyName("speciesManagedByRole")]
    public List<ManagedSpeciesDocument> SpeciesManagedByRole { get; set; } = [];

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static PartyRoleDocument FromDomain(PartyRole m) => new()
    {
        IdentifierId = m.Id,
        Role = PartyRoleRoleDocument.FromDomain(m.Role),
        SpeciesManagedByRole = [.. m.SpeciesManagedByRole.Select(ManagedSpeciesDocument.FromDomain)],
        LastUpdatedDate = m.LastUpdatedDate
    };

    public PartyRole ToDomain()
    {
        var species = SpeciesManagedByRole.Select(s => s.ToDomain());

        return new PartyRole(
            IdentifierId,
            null,
            Role.ToDomain(),
            species,
            LastUpdatedDate
        );
    }
}