using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class CtsCphHolding : BronzeBase
{
    [JsonPropertyName("LID_FULL_IDENTIFIER")]
    public string LID_FULL_IDENTIFIER { get; set; } = string.Empty;

    [JsonPropertyName("LTY_LOC_TYPE")]
    public string LTY_LOC_TYPE { get; set; } = string.Empty;

    [JsonPropertyName("ADR_NAME")]
    public string? ADR_NAME { get; set; }

    [JsonPropertyName("ADR_ADDRESS_2")]
    public string? ADR_ADDRESS_2 { get; set; }

    [JsonPropertyName("ADR_ADDRESS_3")]
    public string? ADR_ADDRESS_3 { get; set; }

    [JsonPropertyName("ADR_ADDRESS_4")]
    public string? ADR_ADDRESS_4 { get; set; }

    [JsonPropertyName("ADR_ADDRESS_5")]
    public string? ADR_ADDRESS_5 { get; set; }

    [JsonPropertyName("ADR_POST_CODE")]
    public string? ADR_POST_CODE { get; set; }

    [JsonPropertyName("LOC_TEL_NUMBER")]
    public string? LOC_TEL_NUMBER { get; set; }

    [JsonPropertyName("LOC_MOBILE_NUMBER")]
    public string? LOC_MOBILE_NUMBER { get; set; }

    [JsonPropertyName("LOC_MAP_REFERENCE")]
    public string? LOC_MAP_REFERENCE { get; set; }

    [JsonPropertyName("LOC_EFFECTIVE_FROM")]
    [JsonConverter(typeof(SafeDateTimeConverter))]
    public DateTime LOC_EFFECTIVE_FROM { get; set; } = default;

    [JsonPropertyName("LOC_EFFECTIVE_TO")]
    [JsonConverter(typeof(SafeNullableDateTimeConverter))]
    public DateTime? LOC_EFFECTIVE_TO { get; set; }
}