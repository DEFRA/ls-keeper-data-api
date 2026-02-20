namespace KeeperData.Core.Anonymization;

public interface ISamCommonPiiData
{
    string? PERSON_TITLE { get; set; }
    string? PERSON_GIVEN_NAME { get; set; }
    string? PERSON_GIVEN_NAME2 { get; set; }
    string? PERSON_INITIALS { get; set; }
    string? PERSON_FAMILY_NAME { get; set; }
    string? ORGANISATION_NAME { get; set; }
    string? INTERNET_EMAIL_ADDRESS { get; set; }
    string? MOBILE_NUMBER { get; set; }
    string? TELEPHONE_NUMBER { get; set; }
}

public interface ISamCommonPiiAddressData
{
    string? STREET { get; set; }
    string? LOCALITY { get; set; }
    string? TOWN { get; set; }
    string? POSTCODE { get; set; }
    string? PAON_DESCRIPTION { get; set; }
    string? SAON_DESCRIPTION { get; set; }
    string? UDPRN { get; set; }
}