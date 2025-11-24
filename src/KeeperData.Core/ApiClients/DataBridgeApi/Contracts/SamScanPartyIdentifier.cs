using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamScanPartyIdentifier
{
    [JsonPropertyName("PARTY_ID")]
    public string PARTY_ID { get; set; } = string.Empty;
}
