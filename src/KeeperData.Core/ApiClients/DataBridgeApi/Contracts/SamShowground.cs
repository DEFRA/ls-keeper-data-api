using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamShowground : BronzeBase
{
    [JsonPropertyName("CPH")]
    public string CPH { get; set; } = string.Empty;

    [JsonPropertyName("SAON_START_NUMBER")]
    [JsonConverter(typeof(SafeNullableShortConverter))]
    public short? SAON_START_NUMBER { get; set; }

    [JsonPropertyName("SAON_START_NUMBER_SUFFIX")]
    [JsonConverter(typeof(SafeNullableCharConverter))]
    public char? SAON_START_NUMBER_SUFFIX { get; set; }

    [JsonPropertyName("SAON_END_NUMBER")]
    [JsonConverter(typeof(SafeNullableShortConverter))]
    public short? SAON_END_NUMBER { get; set; }

    [JsonPropertyName("SAON_END_NUMBER_SUFFIX")]
    [JsonConverter(typeof(SafeNullableCharConverter))]
    public char? SAON_END_NUMBER_SUFFIX { get; set; }

    [JsonPropertyName("SAON_DESCRIPTION")]
    public string? SAON_DESCRIPTION { get; set; }

    [JsonPropertyName("PAON_START_NUMBER")]
    [JsonConverter(typeof(SafeNullableShortConverter))]
    public short? PAON_START_NUMBER { get; set; }

    [JsonPropertyName("PAON_START_NUMBER_SUFFIX")]
    [JsonConverter(typeof(SafeNullableCharConverter))]
    public char? PAON_START_NUMBER_SUFFIX { get; set; }

    [JsonPropertyName("PAON_END_NUMBER")]
    [JsonConverter(typeof(SafeNullableShortConverter))]
    public short? PAON_END_NUMBER { get; set; }

    [JsonPropertyName("PAON_END_NUMBER_SUFFIX")]
    [JsonConverter(typeof(SafeNullableCharConverter))]
    public char? PAON_END_NUMBER_SUFFIX { get; set; }

    [JsonPropertyName("PAON_DESCRIPTION")]
    public string? PAON_DESCRIPTION { get; set; }

    [JsonPropertyName("STREET")]
    public string? STREET { get; set; }

    [JsonPropertyName("LOCALITY")]
    public string? LOCALITY { get; set; }

    [JsonPropertyName("TOWN")]
    public string? TOWN { get; set; }

    [JsonPropertyName("UK_INTERNAL_CODE")]
    public string? UK_INTERNAL_CODE { get; set; }

    [JsonPropertyName("POSTCODE")]
    public string? POSTCODE { get; set; }

    [JsonPropertyName("COUNTRY_CODE")]
    public string? COUNTRY_CODE { get; set; }

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