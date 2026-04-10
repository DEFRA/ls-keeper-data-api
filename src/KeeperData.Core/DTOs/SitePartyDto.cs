using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A party associated with a site, containing their contact details, address and roles.
/// </summary>
public class SitePartyDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// This is the L or the C number provided from the SAM system.
    /// </summary>
    /// <example>C77473</example>
    [JsonPropertyName("customerNumber")]
    public string CustomerNumber { get; set; } = string.Empty;

    /// <summary>
    /// The title of the party (e.g. Mr, Mrs).
    /// </summary>
    /// <example>Mr</example>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The first name of the party.
    /// </summary>
    /// <example>John</example>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// The last name of the party.
    /// </summary>
    /// <example>Doe</example>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// The full name of the party.
    /// </summary>
    /// <example>John Doe</example>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The type of party (e.g. Person, Organisation).
    /// </summary>
    /// <example>Person</example>
    [JsonPropertyName("partyType")]
    public string? PartyType { get; set; }

    /// <summary>
    /// The current state of the party.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// The timestamp of the last time the Site Party record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// The communication details of the party.
    /// </summary>
    [JsonPropertyName("communication")]
    public List<CommunicationDto> Communication { get; set; } = [];

    /// <summary>
    /// The correspondence address of the party.
    /// </summary>
    [JsonPropertyName("correspondanceAddress")]
    public AddressDto? CorrespondanceAddress { get; set; }

    /// <summary>
    /// The roles assigned to the party.
    /// </summary>
    [JsonPropertyName("partyRoles")]
    public List<PartyRoleDto> PartyRoles { get; set; } = [];
}
