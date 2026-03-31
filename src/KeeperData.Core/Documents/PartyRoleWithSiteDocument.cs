using KeeperData.Core.Domain.Shared;
using KeeperData.Core.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace KeeperData.Core.Documents;

/// <summary>
/// A role assigned to a party, with optional site association and species managed under that role.
/// </summary>
public class PartyRoleWithSiteDocument : INestedEntity
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [BsonElement("id")]
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The site associated with this role.
    /// </summary>
    [BsonElement("site")]
    [JsonPropertyName("site")]
    public PartyRoleSiteDocument? Site { get; set; }

    /// <summary>
    /// The role assigned to the party.
    /// </summary>
    [BsonElement("role")]
    [JsonPropertyName("role")]
    public required PartyRoleRoleDocument Role { get; set; }

    /// <summary>
    /// The list of species managed by the said Role assigned to the said Party.
    /// </summary>
    [BsonElement("speciesManagedByRole")]
    [JsonPropertyName("speciesManagedByRole")]
    public List<ManagedSpeciesDocument> SpeciesManagedByRole { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the Roles To Party record was updated.
    /// </summary>
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