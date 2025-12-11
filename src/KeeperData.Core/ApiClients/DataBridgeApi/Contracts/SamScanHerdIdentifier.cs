using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamScanHerdIdentifier
{
    [JsonPropertyName("CPHH")]
    public string CPHH { get; set; } = string.Empty;
}