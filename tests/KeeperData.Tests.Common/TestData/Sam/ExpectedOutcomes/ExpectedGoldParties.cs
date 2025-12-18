using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;

namespace KeeperData.Tests.Common.TestData.Sam.ExpectedOutcomes;

public static class ExpectedGoldParties
{
    private static readonly string s_siteId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    private static readonly string s_cphNumber = "12/345/6789";

    public static List<PartyDocument> DefaultExpectedParties =>
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
                State = PartyStatusType.Active.GetDescription(),
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
                    Postcode = "M20 2XY",
                    Country = CountryData.GetSummary("GB-ENG"),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },

                PartyRoles =
                [
                    new PartyRoleWithSiteDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Site = new PartyRoleSiteDocument
                        {
                            IdentifierId = s_siteId,
                            Name = "North Market Farm",
                            State = HoldingStatusType.Active.GetDescription(),
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
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        },
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
                    new PartyRoleWithSiteDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Site = new PartyRoleSiteDocument
                        {
                            IdentifierId = s_siteId,
                            Name = "North Market Farm",
                            State = HoldingStatusType.Active.GetDescription(),
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
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        },
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
                    new PartyRoleWithSiteDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Site = new PartyRoleSiteDocument
                        {
                            IdentifierId = s_siteId,
                            Name = "North Market Farm",
                            State = HoldingStatusType.Active.GetDescription(),
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
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        },
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
                Id = Guid.NewGuid().ToString(),
                CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                Title = "Mrs",
                FirstName = "Jane",
                LastName = "Doe",
                Name = "Mrs Jane D Doe",
                CustomerNumber = "C1000002",
                PartyType = "Person",
                State = PartyStatusType.Active.GetDescription(),
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
                    Postcode = "M20 2XY",
                    Country = CountryData.GetSummary("GB-ENG"),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },

                PartyRoles =
                [
                    new PartyRoleWithSiteDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Site = new PartyRoleSiteDocument
                        {
                            IdentifierId = s_siteId,
                            Name = "North Market Farm",
                            State = HoldingStatusType.Active.GetDescription(),
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
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        },
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
        ];

    public static List<PartyDocument> ExpectedParties_UpdatedHolderAndParties =>
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
                State = PartyStatusType.Active.GetDescription(),
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
                    Postcode = "M20 2XY",
                    Country = CountryData.GetSummary("GB-ENG"),
                    LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                },

                PartyRoles =
                [
                    new PartyRoleWithSiteDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Site = new PartyRoleSiteDocument
                        {
                            IdentifierId = s_siteId,
                            Name = "North Market Farm",
                            State = HoldingStatusType.Active.GetDescription(),
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
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        },
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
                Id = Guid.NewGuid().ToString(),
                CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0),
                LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0),
                Title = "Mr",
                FirstName = "Dave",
                LastName = "Smith",
                Name = "Mr Dave Smith",
                CustomerNumber = "C1000005",
                PartyType = "Person",
                State = PartyStatusType.Active.GetDescription(),
                Deleted = false,

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
                    new PartyRoleWithSiteDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Site = new PartyRoleSiteDocument
                        {
                            IdentifierId = s_siteId,
                            Name = "North Market Farm",
                            State = HoldingStatusType.Active.GetDescription(),
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
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        },
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
                    new PartyRoleWithSiteDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Site = new PartyRoleSiteDocument
                        {
                            IdentifierId = s_siteId,
                            Name = "North Market Farm",
                            State = HoldingStatusType.Active.GetDescription(),
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
                            LastUpdatedDate = new DateTime(2025, 2, 2, 0, 0, 0)
                        },
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
        ];
}