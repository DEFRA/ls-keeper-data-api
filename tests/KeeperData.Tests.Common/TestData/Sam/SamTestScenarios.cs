using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Tests.Common.TestData.Sam;

public static class SamTestScenarios
{
    private static readonly string s_cphNumber = "12/345/6789";

    public static SamTestScenarioData DefaultScenario => new()
    {
        Cph = s_cphNumber,
        RawHoldings = [
            new()
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

                FACILITY_BUSINSS_ACTVTY_CODE = "RM",
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
                FCLTY_SUB_BSNSS_ACTVTY_CODE = null,
                FEATURE_STATUS_CODE = null,
                MOVEMENT_RSTRCTN_RSN_CODE = null
            },
            new()
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

                FACILITY_BUSINSS_ACTVTY_CODE = "WM",
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
                FCLTY_SUB_BSNSS_ACTVTY_CODE = null,
                FEATURE_STATUS_CODE = null,
                MOVEMENT_RSTRCTN_RSN_CODE = null
            }
        ],
        RawHerds = [
            new()
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
            },
            new()
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
            }
        ],
        RawHolders = [
            new()
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
                PAON_DESCRIPTION = null
            }
        ],
        RawParties = [
            new()
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
            },
            new()
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
            }
        ],
        ExpectedGoldSite = DefaultExpectedSite,
        ExpectedGoldParties = DefaultExpectedParties,
        ExpectedGoldSitePartyRoles = DefaultExpectedSitePartyRoles,
        ExpectedGoldSiteGroupMarks = DefaultExpectedSiteGroupMarks
    };

    private static SiteDocument DefaultExpectedSite =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            Type = "AH",
            Name = "North Market Farm",
            State = HoldingStatusType.Active.ToString(),
            StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
            EndDate = null,
            Source = SourceSystemType.SAM.ToString(),
            DestroyIdentityDocumentsFlag = null,
            Deleted = false,

            Location = new LocationDocument()
            {
                IdentifierId = Guid.NewGuid().ToString(),
                OsMapReference = "ND2150071600",
                Easting = 399568,
                Northing = 579087,

                Address = new AddressDocument()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Uprn = 25962203,
                    AddressLine1 = "1-3, 10-12",
                    AddressLine2 = "Market Square",
                    PostTown = "Oxford",
                    County = "North Oxford",
                    PostCode = "OX1 3EQ",
                    Country = CountryData.GetSummary("GB"),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },

                Communication = [
                    new()
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Email = null,
                        Mobile = null,
                        Landline = null,
                        PrimaryContactFlag = false,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    }
                ],

                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
            },

            Identifiers =
            [
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Identifier = s_cphNumber,
                    Type = HoldingIdentifierType.CphNumber.ToString(),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                }
            ],

            Parties =
            [
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    PartyId = "C1000001",
                    Title = "Mr",
                    FirstName = "John James",
                    LastName = "Doe",
                    Name = "Mr John James P Doe",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),

                    Communication =
                    [
                        new CommunicationDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            Email = "john.doe@email.co.uk",
                            Mobile = "07795801234",
                            Landline = "0191 123 4567",
                            PrimaryContactFlag = false,
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        }
                    ],

                    CorrespondanceAddress = new AddressDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Uprn = 25962203,
                        AddressLine1 = "1-3, 10-12",
                        AddressLine2 = "Elm Grove",
                        PostTown = "Manchester",
                        County = "West Didsbury",
                        PostCode = "M20 2XY",
                        Country = CountryData.GetSummary("GB"),
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },

                    PartyRoles =
                    [
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            RoleId = RoleData.Find("LIVESTOCKOWNER").id!,
                            Role = RoleData.Find("LIVESTOCKOWNER").name!,
                            StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                            EndDate = null,
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            SpeciesManagedByRole =
                            [
                                new()
                                {
                                    IdentifierId = Guid.NewGuid().ToString(),
                                    Code = "CTT",
                                    Name = SpeciesData.Find("CTT").name!,
                                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                    EndDate = null,
                                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                                },
                                new()
                                {
                                    IdentifierId = Guid.NewGuid().ToString(),
                                    Code = "SHP",
                                    Name = SpeciesData.Find("SHP").name!,
                                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                    EndDate = null,
                                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                                }
                            ]
                        },
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            RoleId = RoleData.Find("LIVESTOCKKEEPER").id!,
                            Role = RoleData.Find("LIVESTOCKKEEPER").name!,
                            StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                            EndDate = null,
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            SpeciesManagedByRole =
                            [
                                new()
                                {
                                    IdentifierId = Guid.NewGuid().ToString(),
                                    Code = "CTT",
                                    Name = SpeciesData.Find("CTT").name!,
                                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                    EndDate = null,
                                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                                },
                                new()
                                {
                                    IdentifierId = Guid.NewGuid().ToString(),
                                    Code = "SHP",
                                    Name = SpeciesData.Find("SHP").name!,
                                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                    EndDate = null,
                                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                                }
                            ]
                        },
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            RoleId = RoleData.Find("CPHHOLDER").id!,
                            Role = RoleData.Find("CPHHOLDER").name!,
                            StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                            EndDate = null,
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            SpeciesManagedByRole =
                            [
                                new()
                                {
                                    IdentifierId = Guid.NewGuid().ToString(),
                                    Code = "CTT",
                                    Name = SpeciesData.Find("CTT").name!,
                                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                    EndDate = null,
                                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                                },
                                new()
                                {
                                    IdentifierId = Guid.NewGuid().ToString(),
                                    Code = "SHP",
                                    Name = SpeciesData.Find("SHP").name!,
                                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                    EndDate = null,
                                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                                }
                            ]
                        }
                    ]
                },
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    PartyId = "C1000002",
                    Title = "Mrs",
                    FirstName = "Jane",
                    LastName = "Doe",
                    Name = "Mrs Jane D Doe",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),

                    Communication =
                    [
                        new CommunicationDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            Email = "jane.doe@email.co.uk",
                            Mobile = "07795801234",
                            Landline = "0191 123 4567",
                            PrimaryContactFlag = false,
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        }
                    ],

                    CorrespondanceAddress = new AddressDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Uprn = 25962203,
                        AddressLine1 = "1-3, 10-12",
                        AddressLine2 = "Elm Grove",
                        PostTown = "Manchester",
                        County = "West Didsbury",
                        PostCode = "M20 2XY",
                        Country = CountryData.GetSummary("GB"),
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },

                    PartyRoles =
                    [
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            RoleId = RoleData.Find("LIVESTOCKKEEPER").id!,
                            Role = RoleData.Find("LIVESTOCKKEEPER").name!,
                            StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                            EndDate = null,
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            SpeciesManagedByRole =
                            [
                                new()
                                {
                                    IdentifierId = Guid.NewGuid().ToString(),
                                    Code = "CTT",
                                    Name = SpeciesData.Find("CTT").name!,
                                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                    EndDate = null,
                                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                                },
                                new()
                                {
                                    IdentifierId = Guid.NewGuid().ToString(),
                                    Code = "SHP",
                                    Name = SpeciesData.Find("SHP").name!,
                                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                    EndDate = null,
                                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                                }
                            ]
                        }
                    ]
                }
            ],

            Species =
            [
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Code = "CTT",
                    Name = SpeciesData.Find("CTT").name!,
                    LastModifiedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Code = "SHP",
                    Name = SpeciesData.Find("SHP").name!,
                    LastModifiedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                }
            ],

            Marks =
            [
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Mark = "H1000001",
                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                    EndDate = null,
                    Species = new()
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Code = "CTT",
                        Name = SpeciesData.Find("CTT").name!,
                        LastModifiedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                },
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Mark = "H1000002",
                    StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                    EndDate = null,
                    Species = new()
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Code = "SHP",
                        Name = SpeciesData.Find("SHP").name!,
                        LastModifiedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                }
            ],

            SiteActivities =
            [
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Activity = "RM",
                    Description = PremiseActivityTypeData.Find("RM").name!,
                    StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                    EndDate = null,
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Activity = "WM",
                    Description = PremiseActivityTypeData.Find("WM").name!,
                    StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                    EndDate = null,
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                }
            ],

            Activities = 
            [
                PremiseActivityTypeData.Find("RM").name!,
                PremiseActivityTypeData.Find("WM").name!
            ]
        };

    private static List<PartyDocument> DefaultExpectedParties =>
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                Title = "Mr",
                FirstName = "John James",
                LastName = "Doe",
                Name = "Mr John James P Doe",
                CustomerNumber = "C1000001",
                PartyType = "Person",
                State = "Active",
                Deleted = false,

                Communication =
                [
                    new CommunicationDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Email = "john.doe@email.co.uk",
                        Mobile = "07795801234",
                        Landline = "0191 123 4567",
                        PrimaryContactFlag = false,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    }
                ],

                CorrespondanceAddress = new AddressDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Uprn = 25962203,
                    AddressLine1 = "1-3, 10-12",
                    AddressLine2 = "Elm Grove",
                    PostTown = "Manchester",
                    County = "West Didsbury",
                    PostCode = "M20 2XY",
                    Country = CountryData.GetSummary("GB"),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },

                PartyRoles =
                [
                    new PartyRoleDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        RoleId = RoleData.Find("LIVESTOCKOWNER").id!,
                        Role = RoleData.Find("LIVESTOCKOWNER").name!,
                        StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                        EndDate = null,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                        SpeciesManagedByRole =
                        [
                            new()
                            {
                                IdentifierId = Guid.NewGuid().ToString(),
                                Code = "CTT",
                                Name = SpeciesData.Find("CTT").name!,
                                StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                EndDate = null,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                            },
                            new()
                            {
                                IdentifierId = Guid.NewGuid().ToString(),
                                Code = "SHP",
                                Name = SpeciesData.Find("SHP").name!,
                                StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                EndDate = null,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                            }
                        ]
                    },
                    new PartyRoleDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        RoleId = RoleData.Find("LIVESTOCKKEEPER").id!,
                        Role = RoleData.Find("LIVESTOCKKEEPER").name!,
                        StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                        EndDate = null,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                        SpeciesManagedByRole =
                        [
                            new()
                            {
                                IdentifierId = Guid.NewGuid().ToString(),
                                Code = "CTT",
                                Name = SpeciesData.Find("CTT").name!,
                                StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                EndDate = null,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                            },
                            new()
                            {
                                IdentifierId = Guid.NewGuid().ToString(),
                                Code = "SHP",
                                Name = SpeciesData.Find("SHP").name!,
                                StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                EndDate = null,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                            }
                        ]
                    },
                    new PartyRoleDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        RoleId = RoleData.Find("CPHHOLDER").id!,
                        Role = RoleData.Find("CPHHOLDER").name!,
                        StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                        EndDate = null,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                        SpeciesManagedByRole =
                        [
                            new()
                            {
                                IdentifierId = Guid.NewGuid().ToString(),
                                Code = "CTT",
                                Name = SpeciesData.Find("CTT").name!,
                                StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                EndDate = null,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                            },
                            new()
                            {
                                IdentifierId = Guid.NewGuid().ToString(),
                                Code = "SHP",
                                Name = SpeciesData.Find("SHP").name!,
                                StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                EndDate = null,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                            }
                        ]
                    }
                ]
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                Title = "Mrs",
                FirstName = "Jane",
                LastName = "Doe",
                Name = "Mrs Jane D Doe",
                CustomerNumber = "C1000002",
                PartyType = "Person",
                State = "Active",
                Deleted = false,

                Communication =
                [
                    new CommunicationDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Email = "jane.doe@email.co.uk",
                        Mobile = "07795801234",
                        Landline = "0191 123 4567",
                        PrimaryContactFlag = false,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    }
                ],

                CorrespondanceAddress = new AddressDocument
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Uprn = 25962203,
                    AddressLine1 = "1-3, 10-12",
                    AddressLine2 = "Elm Grove",
                    PostTown = "Manchester",
                    County = "West Didsbury",
                    PostCode = "M20 2XY",
                    Country = CountryData.GetSummary("GB"),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },

                PartyRoles =
                [
                    new PartyRoleDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        RoleId = RoleData.Find("LIVESTOCKKEEPER").id!,
                        Role = RoleData.Find("LIVESTOCKKEEPER").name!,
                        StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                        EndDate = null,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                        SpeciesManagedByRole =
                        [
                            new()
                            {
                                IdentifierId = Guid.NewGuid().ToString(),
                                Code = "CTT",
                                Name = SpeciesData.Find("CTT").name!,
                                StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                EndDate = null,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                            },
                            new()
                            {
                                IdentifierId = Guid.NewGuid().ToString(),
                                Code = "SHP",
                                Name = SpeciesData.Find("SHP").name!,
                                StartDate = new DateTime(2005, 1, 1, 0, 0, 0),
                                EndDate = null,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                            }
                        ]
                    }
                ]
            }
        ];

    private static List<Core.Documents.SitePartyRoleRelationshipDocument> DefaultExpectedSitePartyRoles =>
    [
        new()
        {
            Id = Guid.NewGuid().ToString(),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKOWNER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKOWNER").name!,
            EffectiveFromData = new DateTime(2001, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            EffectiveFromData = new DateTime(2001, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("CPHHOLDER").id!,
            RoleTypeName = RoleData.Find("CPHHOLDER").name!,
            EffectiveFromData = new DateTime(2001, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKOWNER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKOWNER").name!,
            EffectiveFromData = new DateTime(2001, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            EffectiveFromData = new DateTime(2001, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("CPHHOLDER").id!,
            RoleTypeName = RoleData.Find("CPHHOLDER").name!,
            EffectiveFromData = new DateTime(2001, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            PartyId = "C1000002",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            EffectiveFromData = new DateTime(2001, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            PartyId = "C1000002",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            EffectiveFromData = new DateTime(2001, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP"
        }
    ];

    private static List<SiteGroupMarkRelationshipDocument> DefaultExpectedSiteGroupMarks =>
    [
        new()
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            Herdmark = "H1000001",
            CountyParishHoldingHerd = $"{s_cphNumber}/01",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKOWNER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKOWNER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT",
            SpeciesTypeName = SpeciesData.Find("CTT").name!,
            ProductionUsageId = ProductionUsageData.Find("BEEF").id!,
            ProductionUsageCode = "BEEF",
            ProductionTypeId = null,
            ProductionTypeCode = null,
            DiseaseType = null,
            Interval = 6,
            IntervalUnitOfTime = "Months",
            GroupMarkStartDate = new DateTime(2005, 1, 1, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            Herdmark = "H1000001",
            CountyParishHoldingHerd = $"{s_cphNumber}/01",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT",
            SpeciesTypeName = SpeciesData.Find("CTT").name!,
            ProductionUsageId = ProductionUsageData.Find("BEEF").id!,
            ProductionUsageCode = "BEEF",
            ProductionTypeId = null,
            ProductionTypeCode = null,
            DiseaseType = null,
            Interval = 6,
            IntervalUnitOfTime = "Months",
            GroupMarkStartDate = new DateTime(2005, 1, 1, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            Herdmark = "H1000001",
            CountyParishHoldingHerd = $"{s_cphNumber}/01",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("CPHHOLDER").id!,
            RoleTypeName = RoleData.Find("CPHHOLDER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT",
            SpeciesTypeName = SpeciesData.Find("CTT").name!,
            ProductionUsageId = ProductionUsageData.Find("BEEF").id!,
            ProductionUsageCode = "BEEF",
            ProductionTypeId = null,
            ProductionTypeCode = null,
            DiseaseType = null,
            Interval = 6,
            IntervalUnitOfTime = "Months",
            GroupMarkStartDate = new DateTime(2005, 1, 1, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            Herdmark = "H1000002",
            CountyParishHoldingHerd = $"{s_cphNumber}/01",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKOWNER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKOWNER").name!,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP",
            SpeciesTypeName = SpeciesData.Find("SHP").name!,
            ProductionUsageId = ProductionUsageData.Find("MEAT").id!,
            ProductionUsageCode = "MEAT",
            ProductionTypeId = null,
            ProductionTypeCode = null,
            DiseaseType = null,
            Interval = 12,
            IntervalUnitOfTime = "Months",
            GroupMarkStartDate = new DateTime(2005, 1, 1, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            Herdmark = "H1000002",
            CountyParishHoldingHerd = $"{s_cphNumber}/01",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP",
            SpeciesTypeName = SpeciesData.Find("SHP").name!,
            ProductionUsageId = ProductionUsageData.Find("MEAT").id!,
            ProductionUsageCode = "MEAT",
            ProductionTypeId = null,
            ProductionTypeCode = null,
            DiseaseType = null,
            Interval = 12,
            IntervalUnitOfTime = "Months",
            GroupMarkStartDate = new DateTime(2005, 1, 1, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            PartyId = "C1000001",
            PartyTypeId = "Person",
            Herdmark = "H1000002",
            CountyParishHoldingHerd = $"{s_cphNumber}/01",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("CPHHOLDER").id!,
            RoleTypeName = RoleData.Find("CPHHOLDER").name!,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP",
            SpeciesTypeName = SpeciesData.Find("SHP").name!,
            ProductionUsageId = ProductionUsageData.Find("MEAT").id!,
            ProductionUsageCode = "MEAT",
            ProductionTypeId = null,
            ProductionTypeCode = null,
            DiseaseType = null,
            Interval = 12,
            IntervalUnitOfTime = "Months",
            GroupMarkStartDate = new DateTime(2005, 1, 1, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            PartyId = "C1000002",
            PartyTypeId = "Person",
            Herdmark = "H1000001",
            CountyParishHoldingHerd = $"{s_cphNumber}/01",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT",
            SpeciesTypeName = SpeciesData.Find("CTT").name!,
            ProductionUsageId = ProductionUsageData.Find("BEEF").id!,
            ProductionUsageCode = "BEEF",
            ProductionTypeId = null,
            ProductionTypeCode = null,
            DiseaseType = null,
            Interval = 6,
            IntervalUnitOfTime = "Months",
            GroupMarkStartDate = new DateTime(2005, 1, 1, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            PartyId = "C1000002",
            PartyTypeId = "Person",
            Herdmark = "H1000002",
            CountyParishHoldingHerd = $"{s_cphNumber}/01",
            HoldingIdentifier = s_cphNumber,
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP",
            SpeciesTypeName = SpeciesData.Find("SHP").name!,
            ProductionUsageId = ProductionUsageData.Find("MEAT").id!,
            ProductionUsageCode = "MEAT",
            ProductionTypeId = null,
            ProductionTypeCode = null,
            DiseaseType = null,
            Interval = 12,
            IntervalUnitOfTime = "Months",
            GroupMarkStartDate = new DateTime(2005, 1, 1, 0, 0, 0),
            GroupMarkEndDate = null
        }
    ];

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
}
