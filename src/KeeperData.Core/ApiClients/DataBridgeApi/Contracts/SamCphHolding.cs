using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamCphHolding : BronzeBase
{
    [JsonPropertyName("CPH")]
    public string CPH { get; set; } = string.Empty;

    [JsonPropertyName("FEATURE_NAME")]
    public string FEATURE_NAME { get; set; } = string.Empty;

    [JsonPropertyName("CPH_TYPE")]
    public string CPH_TYPE { get; set; } = string.Empty;

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

    [JsonPropertyName("TOWN")]
    public string? TOWN { get; set; }

    [JsonPropertyName("LOCALITY")]
    public string? LOCALITY { get; set; }

    [JsonPropertyName("POSTCODE")]
    public string? POSTCODE { get; set; }

    [JsonPropertyName("UK_INTERNAL_CODE")]
    public string? UK_INTERNAL_CODE { get; set; }

    [JsonPropertyName("COUNTRY_CODE")]
    public string? COUNTRY_CODE { get; set; }

    [JsonPropertyName("UDPRN")]
    public string? UDPRN { get; set; }

    [JsonPropertyName("EASTING")]
    [JsonConverter(typeof(SafeNullableIntConverter))]
    public int? EASTING { get; set; }

    [JsonPropertyName("NORTHING")]
    [JsonConverter(typeof(SafeNullableIntConverter))]
    public int? NORTHING { get; set; }

    [JsonPropertyName("OS_MAP_REFERENCE")]
    public string? OS_MAP_REFERENCE { get; set; }

    [JsonPropertyName("DISEASE_TYPE")]
    public string? DISEASE_TYPE { get; set; }

    [JsonPropertyName("INTERVAL")]
    [JsonConverter(typeof(SafeNullableDecimalConverter))]
    public decimal? INTERVAL { get; set; }

    [JsonPropertyName("INTERVAL_UNIT_OF_TIME")]
    public string? INTERVAL_UNIT_OF_TIME { get; set; }

    [JsonPropertyName("FEATURE_ADDRESS_FROM_DATE")]
    [JsonConverter(typeof(SafeDateTimeConverter))]
    public DateTime FEATURE_ADDRESS_FROM_DATE { get; set; } = default;

    [JsonPropertyName("FEATURE_ADDRESS_TO_DATE")]
    [JsonConverter(typeof(SafeNullableDateTimeConverter))]
    public DateTime? FEATURE_ADDRESS_TO_DATE { get; set; }

    [JsonPropertyName("CPH_RELATIONSHIP_TYPE")]
    public string? CPH_RELATIONSHIP_TYPE { get; set; }

    [JsonPropertyName("SECONDARY_CPH")]
    public string? SECONDARY_CPH { get; set; } = string.Empty;

    [JsonPropertyName("FACILITY_BUSINSS_ACTVTY_CODE")]
    public string? FACILITY_BUSINSS_ACTVTY_CODE { get; set; }

    [JsonPropertyName("FACILITY_TYPE_CODE")]
    public string? FACILITY_TYPE_CODE { get; set; }

    [JsonPropertyName("FCLTY_SUB_BSNSS_ACTVTY_CODE")]
    public string? FCLTY_SUB_BSNSS_ACTVTY_CODE { get; set; }

    [JsonPropertyName("MOVEMENT_RSTRCTN_RSN_CODE")]
    public string? MOVEMENT_RSTRCTN_RSN_CODE { get; set; }

    [JsonPropertyName("ANIMAL_SPECIES_CODE")]
    public string ANIMAL_SPECIES_CODE { get; set; } = string.Empty;

    [JsonPropertyName("ANIMAL_PRODUCTION_USAGE_CODE")]
    public string ANIMAL_PRODUCTION_USAGE_CODE { get; set; } = string.Empty;

    public string SecondaryCphUnwrapped => UnwrapCoalesced(SECONDARY_CPH);
    public string AnimalSpeciesCodeUnwrapped => UnwrapCoalesced(ANIMAL_SPECIES_CODE);
    public List<string> AnimalProductionUsageCodeList => SplitCommaSeparatedIds(UnwrapCoalesced(ANIMAL_PRODUCTION_USAGE_CODE));
}