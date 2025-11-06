namespace KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

public class SamParty : BronzeBase
{
    public string PARTY_ID { get; set; } = string.Empty;

    public string? PERSON_TITLE { get; set; }
    public string? PERSON_GIVEN_NAME { get; set; }
    public string? PERSON_GIVEN_NAME2 { get; set; }
    public string? PERSON_INITIALS { get; set; }
    public string? PERSON_FAMILY_NAME { get; set; }
    public string? ORGANISATION_NAME { get; set; }
    public string? TELEPHONE_NUMBER { get; set; }
    public string? MOBILE_NUMBER { get; set; }
    public string? INTERNET_EMAIL_ADDRESS { get; set; }

    public decimal? ADDRESS_PK { get; set; }

    public short? SAON_START_NUMBER { get; set; }
    public char? SAON_START_NUMBER_SUFFIX { get; set; }
    public short? SAON_END_NUMBER { get; set; }
    public char? SAON_END_NUMBER_SUFFIX { get; set; }
    public string? SAON_DESCRIPTION { get; set; }

    public short? PAON_START_NUMBER { get; set; }
    public char? PAON_START_NUMBER_SUFFIX { get; set; }
    public short? PAON_END_NUMBER { get; set; }
    public char? PAON_END_NUMBER_SUFFIX { get; set; }
    public string? PAON_DESCRIPTION { get; set; }

    public string? STREET { get; set; }
    public string? TOWN { get; set; }
    public string? LOCALITY { get; set; }
    public string? UK_INTERNAL_CODE { get; set; }
    public string? POSTCODE { get; set; }
    public string? COUNTRY_CODE { get; set; }
    public string? UDPRN { get; set; }
    public char? PREFERRED_CONTACT_METHOD_IND { get; set; } = default;

    public string? ROLES { get; set; }

    public DateTime PARTY_ROLE_FROM_DATE { get; set; } = default;
    public DateTime? PARTY_ROLE_TO_DATE { get; set; }
}