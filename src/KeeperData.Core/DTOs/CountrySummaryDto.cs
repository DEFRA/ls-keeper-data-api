using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

public class CountrySummaryDto : CountryDTO
{
    /// <summary>
    /// The timestamp of the last time the Country record was updated.
    /// </summary>
    [JsonPropertyName("lastModifiedDate")]
    public DateTime? LastModifiedDate { get; set; }
}