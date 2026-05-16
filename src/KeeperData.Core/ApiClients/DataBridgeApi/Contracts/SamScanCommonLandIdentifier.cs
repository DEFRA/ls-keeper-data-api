using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamScanCommonLandIdentifier
{
    [JsonPropertyName("COMMON_CPH")]
    public string COMMON_CPH { get; set; } = string.Empty;
}
