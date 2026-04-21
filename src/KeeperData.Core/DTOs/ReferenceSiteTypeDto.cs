using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// An isolated site type reference record. Want not to confuse current SiteTypes endpoint which returns site types with their associated activities, 
/// this DTO is used to return only the site type information without the associated activities. 
/// </summary>
public class ReferenceSiteTypeDto
{
    /// <summary>
    /// This is an immutable field which represents the golden key of the reference object.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The site type code.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// The site type name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}