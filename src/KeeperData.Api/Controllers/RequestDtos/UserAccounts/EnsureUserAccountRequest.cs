using System.Text.Json.Serialization;

namespace KeeperData.Api.Controllers.RequestDtos.UserAccounts;

/// <summary>
/// The identity provider claims captured after a successful OIDC ceremony.
/// </summary>
public class EnsureUserAccountRequest
{
    /// <summary>
    /// The identity provider subject claim.
    /// </summary>
    /// <example>9f3a1c2e-0b6d-4f4e-9d2a-7c8b1e5f0a3d</example>
    [JsonPropertyName("sub")]
    public string? Sub { get; set; }

    /// <summary>
    /// The email address claim.
    /// </summary>
    /// <example>jane.farmer@example.com</example>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// The given name claim.
    /// </summary>
    /// <example>Jane</example>
    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    /// <summary>
    /// The family name claim.
    /// </summary>
    /// <example>Farmer</example>
    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }
}
