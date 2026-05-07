using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamScanPortIdentifier
{
    [JsonPropertyName("CPH")]
    public string CPH { get; set; } = string.Empty;
}