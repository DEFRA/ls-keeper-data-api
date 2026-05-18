using System.Text.Json.Serialization;

namespace KeeperData.Core.DTOs;

public class AssociatedHoldingDto
{
    [JsonPropertyName("holdingIdentifier")]
    public string HoldingIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("contiguousFlag")]
    public bool ContiguousFlag { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }
}