using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;

namespace KeeperData.Tests.Common.TestData.Sam.ExpectedOutcomes;

public static class ExpectedGoldSite
{
    private static readonly string s_siteId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    private static readonly string s_cphNumber = "12/345/6789";

    public static SiteDocument DefaultExpectedSite =>
        new()
        {
            Id = s_siteId,
            CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            Type = new PremisesTypeSummaryDocument
            {
                IdentifierId = "d819dc18-f5a1-4d1a-b332-d18f9d1f9227",
                Code = "AH",
                Description = "Agricultural Holding"
            },
            Name = "North Market Farm",
            State = HoldingStatusType.Active.GetDescription(),
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
                    Postcode = "OX1 3EQ",
                    Country = CountryData.GetSummary("GB-ENG"),
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
                    Type = new SiteIdentifierSummaryDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Code = HoldingIdentifierType.CPHN.ToString(),
                        Description = HoldingIdentifierType.CPHN.GetDescription()!,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                }
            ],

            Parties =
            [
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    CustomerNumber = "C1000001",
                    Title = "Mr",
                    FirstName = "John James",
                    LastName = "Doe",
                    Name = "Mr John James P Doe",
                    PartyType = "Person",
                    State = PartyStatusType.Active.GetDescription(),
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
                        Postcode = "M20 2XY",
                        Country = CountryData.GetSummary("GB-ENG"),
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },

                    PartyRoles =
                    [
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            Role = new PartyRoleRoleDocument
                            {
                                IdentifierId = RoleData.Find("LIVESTOCKOWNER").id!,
                                Code = "LIVESTOCKOWNER",
                                Name = RoleData.Find("LIVESTOCKOWNER").name!,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            },
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
                            Role = new PartyRoleRoleDocument
                            {
                                IdentifierId = RoleData.Find("LIVESTOCKKEEPER").id!,
                                Code = "LIVESTOCKKEEPER",
                                Name = RoleData.Find("LIVESTOCKKEEPER").name!,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            },
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
                            Role = new PartyRoleRoleDocument
                            {
                                IdentifierId = RoleData.Find("CPHHOLDER").id!,
                                Code = "CPHHOLDER",
                                Name = RoleData.Find("CPHHOLDER").name!,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            },
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            SpeciesManagedByRole = []
                        }
                    ]
                },
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    CustomerNumber = "C1000002",
                    Title = "Mrs",
                    FirstName = "Jane",
                    LastName = "Doe",
                    Name = "Mrs Jane D Doe",
                    PartyType = "Person",
                    State = PartyStatusType.Active.GetDescription(),
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
                        Postcode = "M20 2XY",
                        Country = CountryData.GetSummary("GB-ENG"),
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },

                    PartyRoles =
                    [
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            Role = new PartyRoleRoleDocument
                            {
                                IdentifierId = RoleData.Find("LIVESTOCKKEEPER").id!,
                                Code = "LIVESTOCKKEEPER",
                                Name = RoleData.Find("LIVESTOCKKEEPER").name!,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            },
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

            Activities =
            [
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Type = new PremisesActivityTypeSummaryDocument {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Code = "RM",
                        Name = PremiseActivityTypeData.Find("RM").name!,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },
                    StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                    EndDate = null,
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    Type = new PremisesActivityTypeSummaryDocument {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Code = "WM",
                        Name = PremiseActivityTypeData.Find("WM").name!,
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },
                    StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
                    EndDate = null,
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                }
            ]
        };

    public static SiteDocument ExpectedSite_UpdatedHolderAndParties =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
            Type = new PremisesTypeSummaryDocument
            {
                IdentifierId = "d819dc18-f5a1-4d1a-b332-d18f9d1f9227",
                Code = "AH",
                Description = "Agricultural Holding"
            },
            Name = "North Market Farm",
            State = HoldingStatusType.Active.GetDescription(),
            StartDate = new DateTime(2001, 1, 1, 0, 0, 0),
            EndDate = null,
            Source = SourceSystemType.SAM.ToString(),
            DestroyIdentityDocumentsFlag = null,
            Deleted = false,

            Location = DefaultExpectedSite.Location,
            Identifiers = DefaultExpectedSite.Identifiers,

            Parties =
            [
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    CustomerNumber = "C1000001",
                    Title = "Mr",
                    FirstName = "John James",
                    LastName = "Doe",
                    Name = "Mr John James P Doe",
                    PartyType = "Person",
                    State = PartyStatusType.Active.GetDescription(),
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
                        Postcode = "M20 2XY",
                        Country = CountryData.GetSummary("GB-ENG"),
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },

                    PartyRoles =
                    [
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            Role = new PartyRoleRoleDocument
                            {
                                IdentifierId = RoleData.Find("LIVESTOCKKEEPER").id!,
                                Code = "LIVESTOCKKEEPER",
                                Name = RoleData.Find("LIVESTOCKKEEPER").name!,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            },
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
                                }
                            ]
                        }
                    ]
                },
                new()
                {
                    IdentifierId = Guid.NewGuid().ToString(),
                    CustomerNumber = "C1000005",
                    Title = "Mr",
                    FirstName = "Dave",
                    LastName = "Smith",
                    Name = "Mr Dave Smith",
                    PartyType = "Person",
                    State = PartyStatusType.Active.GetDescription(),
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),

                    Communication =
                    [
                        new CommunicationDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            Email = "dave.smith@email.co.uk",
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
                        Postcode = "M20 2XY",
                        Country = CountryData.GetSummary("GB-ENG"),
                        LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                    },

                    PartyRoles =
                    [
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            Role = new PartyRoleRoleDocument
                            {
                                IdentifierId = RoleData.Find("CPHHOLDER").id!,
                                Code = "CPHHOLDER",
                                Name = RoleData.Find("CPHHOLDER").name!,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            },
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            SpeciesManagedByRole = []
                        },
                        new PartyRoleDocument
                        {
                            IdentifierId = Guid.NewGuid().ToString(),
                            Role = new PartyRoleRoleDocument
                            {
                                IdentifierId = RoleData.Find("LIVESTOCKOWNER").id!,
                                Code = "LIVESTOCKOWNER",
                                Name = RoleData.Find("LIVESTOCKOWNER").name!,
                                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                            },
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
                                }
                            ]
                        }
                    ]
                }
            ],

            Species = [.. DefaultExpectedSite.Species.Where(x => x.Code == "CTT")],
            Marks = [.. DefaultExpectedSite.Marks.Where(x => x.Species!.Code == "CTT")],
            Activities = [.. DefaultExpectedSite.Activities.Where(x => x.Type.Code == "RM")]
        };
}