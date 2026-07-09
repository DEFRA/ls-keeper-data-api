using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamShowground : SamAddressBase
{
    [JsonPropertyName("CPH")]
    public string CPH { get; set; } = string.Empty;

    [JsonPropertyName("START_DATE")]
    [JsonConverter(typeof(SafeNullableDateTimeConverter))]
    public DateTime? START_DATE { get; set; }

    [JsonPropertyName("END_DATE")]
    [JsonConverter(typeof(SafeNullableDateTimeConverter))]
    public DateTime? END_DATE { get; set; }
}

public class SamScanShowgroundIdentifier
{
    [JsonPropertyName("CPH")]
    public string CPH { get; set; } = string.Empty;
}