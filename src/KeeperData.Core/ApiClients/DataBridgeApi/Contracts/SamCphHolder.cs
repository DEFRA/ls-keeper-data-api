namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamCphHolder
{
    public string PARTY_ID { get; set; } = string.Empty;

    public string? PERSON_TITLE { get; set; }
    public string? PERSON_GIVEN_NAME { get; set; }
    public string? PERSON_GIVEN_NAME2 { get; set; }
    public string? PERSON_FAMILY_NAME { get; set; }
    public string? ORGANISATION_NAME { get; set; }

    public string? TELEPHONE_NUMBER { get; set; }
    public string? MOBILE_NUMBER { get; set; }
    public string? INTERNET_EMAIL_ADDRESS { get; set; }

    public string? SAON_START_NUMBER { get; set; }
    public string? SAON_END_NUMBER { get; set; }

    public string? PAON_START_NUMBER { get; set; }
    public string? PAON_END_NUMBER { get; set; }

    public string? STREET { get; set; }
    public string? TOWN { get; set; }
    public string? LOCALITY { get; set; }
    public string? POSTCODE { get; set; }
    public string? COUNTRY_CODE { get; set; }
    public string? UDPRN { get; set; }

    /// <summary>
    /// CLOB (comma separated list of CPH)
    /// </summary>
    public string? CPHS { get; set; }
}