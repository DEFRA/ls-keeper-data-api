using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamCommonLand : BronzeBase
{
    [JsonPropertyName("COMMON_LAND_PREMISE_ID")]
    public string? COMMON_LAND_PREMISE_ID { get; set; }

    [JsonPropertyName("MAIN_CPH")]
    public string MAIN_CPH { get; set; } = string.Empty;

    [JsonPropertyName("COMMON_CPH")]
    public string COMMON_CPH { get; set; } = string.Empty;

    [JsonPropertyName("BUSINESS_USAGE")]
    public string? BUSINESS_USAGE { get; set; }

    [JsonPropertyName("PREMISES_NAME")]
    public string? PREMISES_NAME { get; set; }

    [JsonPropertyName("ADDRESS_LINE_1")]
    public string? ADDRESS_LINE_1 { get; set; }

    [JsonPropertyName("ADDRESS_LINE_2")]
    public string? ADDRESS_LINE_2 { get; set; }

    [JsonPropertyName("ADDRESS_LINE_3")]
    public string? ADDRESS_LINE_3 { get; set; }

    [JsonPropertyName("LOCAL_AUTH_NAME")]
    public string? LOCAL_AUTH_NAME { get; set; }

    [JsonPropertyName("COUNTRY")]
    public string? COUNTRY { get; set; }

    [JsonPropertyName("POSTCODE")]
    public string? POSTCODE { get; set; }

    [JsonPropertyName("EASTING")]
    public string? EASTING { get; set; }

    [JsonPropertyName("NORTHING")]
    public string? NORTHING { get; set; }

    [JsonPropertyName("LINK_ID")]
    public string? LINK_ID { get; set; }

    [JsonPropertyName("CONTIGUOUS_COMMON")]
    public string? CONTIGUOUS_COMMON { get; set; }

    [JsonPropertyName("START_DATE")]
    public string? START_DATE { get; set; }

    [JsonPropertyName("END_DATE")]
    public string? END_DATE { get; set; }

    public bool IsMainCphPopulated => MAIN_CPH != "-" && !string.IsNullOrWhiteSpace(MAIN_CPH);
}