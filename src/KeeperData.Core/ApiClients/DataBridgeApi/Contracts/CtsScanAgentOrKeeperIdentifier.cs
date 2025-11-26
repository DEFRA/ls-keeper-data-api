using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class CtsScanAgentOrKeeperIdentifier
{
    [JsonPropertyName("PAR_ID")]
    public string PAR_ID { get; set; } = string.Empty;
}