using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A user account with its current CPH association snapshot.
/// </summary>
public class UserAccountDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the user account.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The identity provider subject claim. Null until the user has logged on for the first time.
    /// </summary>
    /// <example>9f3a1c2e-0b6d-4f4e-9d2a-7c8b1e5f0a3d</example>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// The email address of the user.
    /// </summary>
    /// <example>jane.farmer@example.com</example>
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    /// <summary>
    /// The first name of the user.
    /// </summary>
    /// <example>Jane</example>
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    /// <example>Farmer</example>
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// The display name of the user.
    /// </summary>
    /// <example>Jane Farmer</example>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The CPH associations held by the user at the time of the last refresh.
    /// </summary>
    [JsonPropertyName("cphAssociations")]
    public List<CphAssociationDto> CphAssociations { get; set; } = [];

    /// <summary>
    /// The timestamp of the last time the CPH associations were rebuilt from master data.
    /// </summary>
    [JsonPropertyName("associationsRefreshedDate")]
    public DateTime? AssociationsRefreshedDate { get; set; }

    /// <summary>
    /// The timestamp of the last time the user account was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}

/// <summary>
/// An association between a user account and a CPH.
/// </summary>
public class CphAssociationDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the party role which granted
    /// the association in the read model.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The CPH number the user is associated with.
    /// </summary>
    /// <example>57/103/2335</example>
    [JsonPropertyName("cphNumber")]
    public required string CphNumber { get; set; }

    /// <summary>
    /// The role the user holds on the holding.
    /// </summary>
    /// <example>owner</example>
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// The SAM identifier of the party which grants the association.
    /// </summary>
    [JsonPropertyName("partyId")]
    public string? PartyId { get; set; }

    /// <summary>
    /// The read model identifier of the holding the CPH number belongs to.
    /// </summary>
    [JsonPropertyName("holdingId")]
    public string? HoldingId { get; set; }

    /// <summary>
    /// The name of the holding, where the source carries one.
    /// </summary>
    [JsonPropertyName("holdingName")]
    public string? HoldingName { get; set; }
}
