using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class CtsScanHoldingIdentifier
{
    [JsonPropertyName("LID_FULL_IDENTIFIER")]
    public string LID_FULL_IDENTIFIER { get; set; } = string.Empty;
}