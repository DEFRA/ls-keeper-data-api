using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A role assigned to a party, with species managed under that role.
/// </summary>
public class PartyRoleDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

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
    /// The timestamp of the last time the Party Role record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}

/// <summary>
/// A role definition (code and name).
/// </summary>
public class RoleDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// This is the unique code assigned to the Role entity.
    /// </summary>
    /// <example>LIVESTOCKKEEPER</example>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// The name of the role.
    /// </summary>
    /// <example>Livestock Keeper</example>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The timestamp of the last time the Role record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime? LastUpdatedDate { get; set; }
}