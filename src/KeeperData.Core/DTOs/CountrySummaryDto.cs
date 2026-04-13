using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

/// <summary>
/// A lightweight country summary embedded within address records.
/// </summary>
public class CountrySummaryDto: CountryDTO
{
    /// <summary>
    /// The timestamp of the last time the Country record was updated.
    /// </summary>
    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }
}