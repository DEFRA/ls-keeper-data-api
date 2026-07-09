using KeeperData.Core.ApiClients.DataBridgeApi.Converters;
using System.Text.Json.Serialization;
using KeeperData.Core.Anonymization;

namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamCphHolder : SamAddressBase, ISamCommonPiiData, ISamCommonPiiAddressData
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