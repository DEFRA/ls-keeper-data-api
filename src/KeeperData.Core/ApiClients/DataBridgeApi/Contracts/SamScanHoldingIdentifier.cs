using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamScanHoldingIdentifier
{
    [JsonPropertyName("CPH")]
    public string CPH { get; set; } = string.Empty;
}
