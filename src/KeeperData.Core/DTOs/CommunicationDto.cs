using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// Communication details (email, phone numbers) for a location or party.
/// </summary>
public class CommunicationDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the master data object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string IdentifierId { get; set; }

    /// <summary>
    /// The email address.
    /// </summary>
    /// <example>john.doe@somecompany.co.uk</example>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// The mobile phone number.
    /// </summary>
    /// <example>07123456789</example>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// The landline phone number.
    /// </summary>
    /// <example>0114 1231234</example>
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }

    /// <summary>
    /// Indicates whether this is the primary contact.
    /// </summary>
    /// <example>true</example>
    [JsonPropertyName("primaryContactFlag")]
    public bool? PrimaryContactFlag { get; set; }

    /// <summary>
    /// The timestamp of the last time the Communication record was updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedDate")]
    public DateTime LastUpdatedDate { get; set; }
}
