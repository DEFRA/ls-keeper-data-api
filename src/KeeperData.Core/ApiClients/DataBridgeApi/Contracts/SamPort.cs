using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamPort : BronzeBase
{
    [JsonPropertyName("CPH")]
    public string CPH { get; set; } = string.Empty;

    [JsonPropertyName("PREMISES_NAME")]
    public string? PREMISES_NAME { get; set; }

    [JsonPropertyName("ADDRESS_LINE_1")]
    public string? ADDRESS_LINE_1 { get; set; }

    [JsonPropertyName("ADDRESS_LINE_2")]
    public string? ADDRESS_LINE_2 { get; set; }

    [JsonPropertyName("ADDRESS_LINE_3")]
    public string? ADDRESS_LINE_3 { get; set; }

    [JsonPropertyName("POSTCODE")]
    public string? POSTCODE { get; set; }

    [JsonPropertyName("MAP_REFERENCE")]
    public string? MAP_REFERENCE { get; set; }

    [JsonPropertyName("EASTING")]
    [JsonConverter(typeof(SafeNullableIntConverter))]
    public int? EASTING { get; set; }

    [JsonPropertyName("NORTHING")]
    [JsonConverter(typeof(SafeNullableIntConverter))]
    public int? NORTHING { get; set; }
}