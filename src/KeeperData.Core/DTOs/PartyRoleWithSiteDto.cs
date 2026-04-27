using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A role assigned to a party, with optional site association and species managed under that role.
/// </summary>
public class PartyRoleWithSiteDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The site associated with this role.
    /// </summary>
    [JsonPropertyName("site")]
    public PartyRoleSiteDto? Site { get; set; }

    /// <summary>
    /// The role assigned to the party.
    /// </summary>
    [JsonPropertyName("role")]
    public required RoleDto Role { get; set; }

    /// <summary>
    /// The list of species managed by the said Role assigned to the said Party.
    /// </summary>
    [JsonPropertyName("speciesManagedByRole")]
    public List<ManagedSpeciesDto> SpeciesManagedByRole { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the Roles To Party record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}