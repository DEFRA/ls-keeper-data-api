using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

public class PartyRoleWithSiteDocument : INestedEntity
{
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    [BsonElement("site")]
    [JsonPropertyName("site")]
    public PartyRoleSiteDocument? Site { get; set; }

    [BsonElement("role")]
    [JsonPropertyName("role")]
    public required PartyRoleRoleDocument Role { get; set; }

    [BsonElement("speciesManagedByRole")]
    [JsonPropertyName("speciesManagedByRole")]
    public List<ManagedSpeciesDocument> SpeciesManagedByRole { get; set; } = [];

    [BsonElement("lastUpdatedDate")]
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }

    public static PartyRoleWithSiteDocument FromDomain(PartyRole m) => new()
    {
        IdentifierId = m.Id,
        Site = m.Site != null ? PartyRoleSiteDocument.FromDomain(m.Site) : null,
        Role = PartyRoleRoleDocument.FromDomain(m.Role),
        SpeciesManagedByRole = [.. m.SpeciesManagedByRole.Select(ManagedSpeciesDocument.FromDomain)],
        LastUpdatedDate = m.LastUpdatedDate
    };

    public PartyRole ToDomain()
    {
        var species = SpeciesManagedByRole.Select(s => s.ToDomain());

        return new PartyRole(
            IdentifierId,
            Site?.ToDomain(),
            Role.ToDomain(),
            species,
            LastUpdatedDate
        );
    }
}