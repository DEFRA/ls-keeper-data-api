using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Tests.Common.TestData.Sam.ExpectedOutcomes;

namespace KeeperData.Tests.Common.TestData.Sam;

public static class SamTestScenarios
{
    private static readonly string s_cphNumber = "12/345/6789";

    public static SamTestScenarioData DefaultScenario => new()
    {
        Cph = s_cphNumber,
        RawHoldings = [
            SamCphHolding_Cattle_1,
            SamCphHolding_Sheep_1
        ],
        RawHerds = [
            SamHerd_Cattle_1,
            SamHerd_Sheep_1
        ],
        RawHolders = [
            SamCphHolder_C1000001
        ],
        RawParties = [
            SamParty_C1000001,
            SamParty_C1000002
        ],
        ExpectedGoldSite = ExpectedGoldSite.DefaultExpectedSite,
        ExpectedGoldParties = ExpectedGoldParties.DefaultExpectedParties,
        ExpectedGoldSitePartyRoles = ExpectedGoldSitePartyRoles.DefaultExpectedSitePartyRoles,
        ExpectedGoldSiteGroupMarks = ExpectedGoldSiteGroupMarks.DefaultExpectedSiteGroupMarks
    };

    public static SamTestScenarioData Scenario_UpdatedHolderAndParties()
    {
        var p1_updated = SamParty_C1000001;
        p1_updated.ROLES = "LIVESTOCKKEEPER";

        return new()
        {
            Cph = s_cphNumber,
            RawHoldings = [
                SamCphHolding_Cattle_1
            ],
            RawHerds = [
                SamHerd_Cattle_2
            ],
            RawHolders = [
                SamCphHolder_C1000005
            ],
            RawParties = [
                p1_updated,
                SamParty_C1000005
            ],
            ExpectedGoldSite = ExpectedGoldSite.ExpectedSite_UpdatedHolderAndParties,
            ExpectedGoldParties = ExpectedGoldParties.ExpectedParties_UpdatedHolderAndParties,
            ExpectedGoldSitePartyRoles = ExpectedGoldSitePartyRoles.ExpectedSitePartyRoles_UpdatedHolderAndParties,
            ExpectedGoldSiteGroupMarks = ExpectedGoldSiteGroupMarks.ExpectedSiteGroupMarks_UpdatedHolderAndParties
        };
    }

    public class SamTestScenarioData
    {
        public required string Cph { get; init; }

        public List<SamCphHolding> RawHoldings { get; set; } = [];
        public List<SamHerd> RawHerds { get; set; } = [];
        public List<SamCphHolder> RawHolders { get; set; } = [];
        public List<SamParty> RawParties { get; set; } = [];

        public SiteDocument? ExpectedGoldSite { get; set; }
        public List<PartyDocument> ExpectedGoldParties { get; set; } = [];
        public List<Core.Documents.SitePartyRoleRelationshipDocument> ExpectedGoldSitePartyRoles { get; set; } = [];
        public List<SiteGroupMarkRelationshipDocument> ExpectedGoldSiteGroupMarks { get; set; } = [];
    }

    private static SamCphHolding SamCphHolding_Cattle_1 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        CPH = s_cphNumber,
        FEATURE_NAME = "North Market Farm",
        CPH_TYPE = "PERMANENT",

        SAON_START_NUMBER = 1,
        SAON_END_NUMBER = 3,

        PAON_START_NUMBER = 10,
        PAON_END_NUMBER = 12,

        STREET = "Market Square",
        TOWN = "Oxford",
        LOCALITY = "North Oxford",
        POSTCODE = "OX1 3EQ",
        UK_INTERNAL_CODE = "ENGLAND",
        COUNTRY_CODE = "GB",
        UDPRN = "25962203",

        EASTING = 399568,
        NORTHING = 579087,
        OS_MAP_REFERENCE = "ND2150071600",

        FEATURE_ADDRESS_FROM_DATE = new DateTime(2001, 1, 1, 0, 0, 0),
        FEATURE_ADDRESS_TO_DATE = null,

        SECONDARY_CPH = "12/345/9999",

        FACILITY_BUSINSS_ACTVTY_CODE = "SLG-RM",
        FACILITY_TYPE_CODE = "AH",

        ANIMAL_SPECIES_CODE = "CTT",
        ANIMAL_PRODUCTION_USAGE_CODE = "CTT-BEEF",

        // Optional fields left blank for now
        SAON_START_NUMBER_SUFFIX = null,
        SAON_END_NUMBER_SUFFIX = null,
        SAON_DESCRIPTION = null,
        PAON_START_NUMBER_SUFFIX = null,
        PAON_END_NUMBER_SUFFIX = null,
        PAON_DESCRIPTION = null,

        DISEASE_TYPE = null,
        INTERVAL = null,
        INTERVAL_UNIT_OF_TIME = null,
        CPH_RELATIONSHIP_TYPE = null,
        FCLTY_SUB_BSNSS_ACTVTY_CODE = "SLG-RM-AH",
        FEATURE_STATUS_CODE = null,
        MOVEMENT_RSTRCTN_RSN_CODE = null
    };

    private static SamCphHolding SamCphHolding_Sheep_1 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        CPH = s_cphNumber,
        FEATURE_NAME = "North Market Farm",
        CPH_TYPE = "PERMANENT",

        SAON_START_NUMBER = 1,
        SAON_END_NUMBER = 3,

        PAON_START_NUMBER = 10,
        PAON_END_NUMBER = 12,

        STREET = "Market Square",
        TOWN = "Oxford",
        LOCALITY = "North Oxford",
        POSTCODE = "OX1 3EQ",
        UK_INTERNAL_CODE = "ENGLAND",
        COUNTRY_CODE = "GB",
        UDPRN = "25962203",

        EASTING = 399568,
        NORTHING = 579087,
        OS_MAP_REFERENCE = "ND2150071600",

        FEATURE_ADDRESS_FROM_DATE = new DateTime(2001, 1, 1, 0, 0, 0),
        FEATURE_ADDRESS_TO_DATE = null,

        SECONDARY_CPH = "12/345/9999",

        FACILITY_BUSINSS_ACTVTY_CODE = "SLG-WM",
        FACILITY_TYPE_CODE = "AH",

        ANIMAL_SPECIES_CODE = "SHP",
        ANIMAL_PRODUCTION_USAGE_CODE = "SHP-MEAT",

        // Optional fields left blank for now
        SAON_START_NUMBER_SUFFIX = null,
        SAON_END_NUMBER_SUFFIX = null,
        SAON_DESCRIPTION = null,
        PAON_START_NUMBER_SUFFIX = null,
        PAON_END_NUMBER_SUFFIX = null,
        PAON_DESCRIPTION = null,

        DISEASE_TYPE = null,
        INTERVAL = null,
        INTERVAL_UNIT_OF_TIME = null,
        CPH_RELATIONSHIP_TYPE = null,
        FCLTY_SUB_BSNSS_ACTVTY_CODE = "SLG-WM-AH",
        FEATURE_STATUS_CODE = null,
        MOVEMENT_RSTRCTN_RSN_CODE = null
    };

    private static SamHerd SamHerd_Cattle_1 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        HERDMARK = "H1000001",
        CPHH = $"{s_cphNumber}/01",

        ANIMAL_SPECIES_CODE = "CTT",
        ANIMAL_PURPOSE_CODE = "CTT-BEEF-ADLR",

        OWNER_PARTY_IDS = "C1000001",
        KEEPER_PARTY_IDS = "C1000001,C1000002",

        ANIMAL_GROUP_ID_MCH_FRM_DAT = new DateTime(2005, 1, 1, 0, 0, 0),
        ANIMAL_GROUP_ID_MCH_TO_DAT = null,

        INTERVALS = 6,
        INTERVAL_UNIT_OF_TIME = "Months",

        // Optional fields left blank for now
        DISEASE_TYPE = null,
        MOVEMENT_RSTRCTN_RSN_CODE = null
    };

    private static SamHerd SamHerd_Cattle_2 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        HERDMARK = "H1000001",
        CPHH = $"{s_cphNumber}/01",

        ANIMAL_SPECIES_CODE = "CTT",
        ANIMAL_PURPOSE_CODE = "CTT-BEEF-ADLR",

        OWNER_PARTY_IDS = "C1000005",
        KEEPER_PARTY_IDS = "C1000001",

        ANIMAL_GROUP_ID_MCH_FRM_DAT = new DateTime(2005, 1, 1, 0, 0, 0),
        ANIMAL_GROUP_ID_MCH_TO_DAT = null,

        INTERVALS = 6,
        INTERVAL_UNIT_OF_TIME = "Months",

        // Optional fields left blank for now
        DISEASE_TYPE = null,
        MOVEMENT_RSTRCTN_RSN_CODE = null
    };

    private static SamHerd SamHerd_Sheep_1 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        HERDMARK = "H1000002",
        CPHH = $"{s_cphNumber}/01",

        ANIMAL_SPECIES_CODE = "SHP",
        ANIMAL_PURPOSE_CODE = "SHP-MEAT-DLR",

        OWNER_PARTY_IDS = "C1000001",
        KEEPER_PARTY_IDS = "C1000001,C1000002",

        ANIMAL_GROUP_ID_MCH_FRM_DAT = new DateTime(2005, 1, 1, 0, 0, 0),
        ANIMAL_GROUP_ID_MCH_TO_DAT = null,

        INTERVALS = 12,
        INTERVAL_UNIT_OF_TIME = "Months",

        // Optional fields left blank for now
        DISEASE_TYPE = null,
        MOVEMENT_RSTRCTN_RSN_CODE = null
    };

    private static SamCphHolder SamCphHolder_C1000001 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        PARTY_ID = "C1000001",

        PERSON_TITLE = "Mr",
        PERSON_GIVEN_NAME = "John",
        PERSON_GIVEN_NAME2 = "James",
        PERSON_INITIALS = "P",
        PERSON_FAMILY_NAME = "Doe",
        ORGANISATION_NAME = null,

        TELEPHONE_NUMBER = "0191 123 4567",
        MOBILE_NUMBER = "07795801234",
        INTERNET_EMAIL_ADDRESS = "john.doe@email.co.uk",

        SAON_START_NUMBER = 1,
        SAON_END_NUMBER = 3,

        PAON_START_NUMBER = 10,
        PAON_END_NUMBER = 12,

        STREET = "Elm Grove",
        TOWN = "Manchester",
        LOCALITY = "West Didsbury",
        POSTCODE = "M20 2XY",
        UK_INTERNAL_CODE = "ENGLAND",
        COUNTRY_CODE = "GB",
        UDPRN = "25962203",

        PREFERRED_CONTACT_METHOD_IND = 'Y',

        // Optional fields left blank for now
        SAON_START_NUMBER_SUFFIX = null,
        SAON_END_NUMBER_SUFFIX = null,
        SAON_DESCRIPTION = null,
        PAON_START_NUMBER_SUFFIX = null,
        PAON_END_NUMBER_SUFFIX = null,
        PAON_DESCRIPTION = null,

        CPHS = s_cphNumber
    };

    private static SamCphHolder SamCphHolder_C1000005 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        PARTY_ID = "C1000005",

        PERSON_TITLE = "Mr",
        PERSON_GIVEN_NAME = "Dave",
        PERSON_GIVEN_NAME2 = null,
        PERSON_INITIALS = null,
        PERSON_FAMILY_NAME = "Smith",
        ORGANISATION_NAME = null,

        TELEPHONE_NUMBER = "0191 123 4567",
        MOBILE_NUMBER = "07795801234",
        INTERNET_EMAIL_ADDRESS = "dave.smith@email.co.uk",

        SAON_START_NUMBER = 1,
        SAON_END_NUMBER = 3,

        PAON_START_NUMBER = 10,
        PAON_END_NUMBER = 12,

        STREET = "Elm Grove",
        TOWN = "Manchester",
        LOCALITY = "West Didsbury",
        POSTCODE = "M20 2XY",
        UK_INTERNAL_CODE = "ENGLAND",
        COUNTRY_CODE = "GB",
        UDPRN = "25962203",

        CPHS = s_cphNumber
    };

    private static SamParty SamParty_C1000001 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        PARTY_ID = "C1000001",

        PERSON_TITLE = "Mr",
        PERSON_GIVEN_NAME = "John",
        PERSON_GIVEN_NAME2 = "James",
        PERSON_INITIALS = "P",
        PERSON_FAMILY_NAME = "Doe",
        ORGANISATION_NAME = null,

        TELEPHONE_NUMBER = "0191 123 4567",
        MOBILE_NUMBER = "07795801234",
        INTERNET_EMAIL_ADDRESS = "john.doe@email.co.uk",

        SAON_START_NUMBER = 1,
        SAON_END_NUMBER = 3,

        PAON_START_NUMBER = 10,
        PAON_END_NUMBER = 12,

        STREET = "Elm Grove",
        TOWN = "Manchester",
        LOCALITY = "West Didsbury",
        POSTCODE = "M20 2XY",
        UK_INTERNAL_CODE = "ENGLAND",
        COUNTRY_CODE = "GB",
        UDPRN = "25962203",

        PREFERRED_CONTACT_METHOD_IND = 'Y',

        PARTY_ROLE_FROM_DATE = new DateTime(2001, 1, 1, 0, 0, 0),
        PARTY_ROLE_TO_DATE = null,

        ROLES = "LIVESTOCKOWNER,LIVESTOCKKEEPER",

        // Optional fields left blank for now
        SAON_START_NUMBER_SUFFIX = null,
        SAON_END_NUMBER_SUFFIX = null,
        SAON_DESCRIPTION = null,
        PAON_START_NUMBER_SUFFIX = null,
        PAON_END_NUMBER_SUFFIX = null,
        PAON_DESCRIPTION = null
    };

    private static SamParty SamParty_C1000002 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        PARTY_ID = "C1000002",

        PERSON_TITLE = "Mrs",
        PERSON_GIVEN_NAME = "Jane",
        PERSON_GIVEN_NAME2 = null,
        PERSON_INITIALS = "D",
        PERSON_FAMILY_NAME = "Doe",
        ORGANISATION_NAME = null,

        TELEPHONE_NUMBER = "0191 123 4567",
        MOBILE_NUMBER = "07795801234",
        INTERNET_EMAIL_ADDRESS = "jane.doe@email.co.uk",

        SAON_START_NUMBER = 1,
        SAON_END_NUMBER = 3,

        PAON_START_NUMBER = 10,
        PAON_END_NUMBER = 12,

        STREET = "Elm Grove",
        TOWN = "Manchester",
        LOCALITY = "West Didsbury",
        POSTCODE = "M20 2XY",
        UK_INTERNAL_CODE = "ENGLAND",
        COUNTRY_CODE = "GB",
        UDPRN = "25962203",

        PREFERRED_CONTACT_METHOD_IND = 'Y',

        PARTY_ROLE_FROM_DATE = new DateTime(2001, 1, 1, 0, 0, 0),
        PARTY_ROLE_TO_DATE = null,

        ROLES = "LIVESTOCKKEEPER",

        // Optional fields left blank for now
        SAON_START_NUMBER_SUFFIX = null,
        SAON_END_NUMBER_SUFFIX = null,
        SAON_DESCRIPTION = null,
        PAON_START_NUMBER_SUFFIX = null,
        PAON_END_NUMBER_SUFFIX = null,
        PAON_DESCRIPTION = null
    };

    private static SamParty SamParty_C1000005 => new()
    {
        BATCH_ID = 1,
        CHANGE_TYPE = "I",
        CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0),
        UpdatedAtUtc = new DateTime(2025, 2, 2, 0, 0, 0),
        IsDeleted = false,

        PARTY_ID = "C1000005",

        PERSON_TITLE = "Mr",
        PERSON_GIVEN_NAME = "Dave",
        PERSON_GIVEN_NAME2 = null,
        PERSON_INITIALS = null,
        PERSON_FAMILY_NAME = "Smith",
        ORGANISATION_NAME = null,

        TELEPHONE_NUMBER = "0191 123 4567",
        MOBILE_NUMBER = "07795801234",
        INTERNET_EMAIL_ADDRESS = "dave.smith@email.co.uk",

        SAON_START_NUMBER = 1,
        SAON_END_NUMBER = 3,

        PAON_START_NUMBER = 10,
        PAON_END_NUMBER = 12,

        STREET = "Elm Grove",
        TOWN = "Manchester",
        LOCALITY = "West Didsbury",
        POSTCODE = "M20 2XY",
        UK_INTERNAL_CODE = "ENGLAND",
        COUNTRY_CODE = "GB",
        UDPRN = "25962203",

        PREFERRED_CONTACT_METHOD_IND = 'Y',

        PARTY_ROLE_FROM_DATE = new DateTime(2001, 1, 1, 0, 0, 0),
        PARTY_ROLE_TO_DATE = null,

        ROLES = "LIVESTOCKOWNER",

        // Optional fields left blank for now
        SAON_START_NUMBER_SUFFIX = null,
        SAON_END_NUMBER_SUFFIX = null,
        SAON_DESCRIPTION = null,
        PAON_START_NUMBER_SUFFIX = null,
        PAON_END_NUMBER_SUFFIX = null,
        PAON_DESCRIPTION = null
    };
}