using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class CtsAgentOrKeeper : BronzeBase
{
    [JsonPropertyName("PAR_ID")]
    public string PAR_ID { get; set; } = string.Empty;

    [JsonPropertyName("LID_FULL_IDENTIFIER")]
    public string LID_FULL_IDENTIFIER { get; set; } = string.Empty;

    [JsonPropertyName("PAR_TITLE")]
    public string? PAR_TITLE { get; set; }

    [JsonPropertyName("PAR_INITIALS")]
    public string? PAR_INITIALS { get; set; }

    [JsonPropertyName("PAR_SURNAME")]
    public string? PAR_SURNAME { get; set; }

    [JsonPropertyName("PAR_TEL_NUMBER")]
    public string? PAR_TEL_NUMBER { get; set; }

    [JsonPropertyName("PAR_MOBILE_NUMBER")]
    public string? PAR_MOBILE_NUMBER { get; set; }

    [JsonPropertyName("PAR_EMAIL_ADDRESS")]
    public string? PAR_EMAIL_ADDRESS { get; set; }

    [JsonPropertyName("LOC_TEL_NUMBER")]
    public string? LOC_TEL_NUMBER { get; set; }

    [JsonPropertyName("LOC_MOBILE_NUMBER")]
    public string? LOC_MOBILE_NUMBER { get; set; }

    [JsonPropertyName("ADR_NAME")]
    public string ADR_NAME { get; set; } = string.Empty;

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

    [JsonPropertyName("LPR_EFFECTIVE_FROM_DATE")]
    [JsonConverter(typeof(SafeDateTimeConverter))]
    public DateTime LPR_EFFECTIVE_FROM_DATE { get; set; } = default;

    [JsonPropertyName("LPR_EFFECTIVE_TO_DATE")]
    [JsonConverter(typeof(SafeNullableDateTimeConverter))]
    public DateTime? LPR_EFFECTIVE_TO_DATE { get; set; }
}