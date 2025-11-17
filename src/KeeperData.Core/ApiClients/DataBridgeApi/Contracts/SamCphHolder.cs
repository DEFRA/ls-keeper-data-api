using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamCphHolder : BronzeBase
{
    [JsonPropertyName("PARTY_ID")]
    public string PARTY_ID { get; set; } = string.Empty;

    [JsonPropertyName("PERSON_TITLE")]
    public string? PERSON_TITLE { get; set; }

    [JsonPropertyName("PERSON_GIVEN_NAME")]
    public string? PERSON_GIVEN_NAME { get; set; }

    [JsonPropertyName("PERSON_GIVEN_NAME2")]
    public string? PERSON_GIVEN_NAME2 { get; set; }

    [JsonPropertyName("PERSON_INITIALS")]
    public string? PERSON_INITIALS { get; set; }

    [JsonPropertyName("PERSON_FAMILY_NAME")]
    public string? PERSON_FAMILY_NAME { get; set; }

    [JsonPropertyName("ORGANISATION_NAME")]
    public string? ORGANISATION_NAME { get; set; }

    [JsonPropertyName("TELEPHONE_NUMBER")]
    public string? TELEPHONE_NUMBER { get; set; }

    [JsonPropertyName("MOBILE_NUMBER")]
    public string? MOBILE_NUMBER { get; set; }

    [JsonPropertyName("INTERNET_EMAIL_ADDRESS")]
    public string? INTERNET_EMAIL_ADDRESS { get; set; }

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

    [JsonPropertyName("UK_INTERNAL_CODE")]
    public string? UK_INTERNAL_CODE { get; set; }

    [JsonPropertyName("POSTCODE")]
    public string? POSTCODE { get; set; }

    [JsonPropertyName("COUNTRY_CODE")]
    public string? COUNTRY_CODE { get; set; }

    [JsonPropertyName("UDPRN")]
    public string? UDPRN { get; set; }

    [JsonPropertyName("PREFERRED_CONTACT_METHOD_IND")]
    [JsonConverter(typeof(SafeNullableCharConverter))]
    public char? PREFERRED_CONTACT_METHOD_IND { get; set; } = default;

    /// <summary>
    /// CLOB (comma separated list of CPH)
    /// </summary>
    [JsonPropertyName("CPHS")]
    public string? CPHS { get; set; }

    public List<string> CphList => SplitCommaSeparatedIds(CPHS ?? string.Empty);
}