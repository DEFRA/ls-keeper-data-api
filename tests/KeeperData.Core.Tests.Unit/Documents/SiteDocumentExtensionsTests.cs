using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;

namespace KeeperData.Core.Tests.Unit.Documents;

public class SiteDocumentExtensionsTests
{
    [Fact]
    public void ToDto_WithFullSiteDocument_ShouldMapAllProperties()
    {
        // Arrange
        var siteDocument = CreateFullSiteDocument();

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(siteDocument.Id);
        result.LastUpdatedDate.Should().Be(siteDocument.LastUpdatedDate);
        result.Name.Should().Be(siteDocument.Name);
        result.State.Should().Be(siteDocument.State);
        result.StartDate.Should().Be(siteDocument.StartDate);
        result.EndDate.Should().Be(siteDocument.EndDate);
        result.Source.Should().Be(siteDocument.Source);
        result.DestroyIdentityDocumentsFlag.Should().Be(siteDocument.DestroyIdentityDocumentsFlag);
        result.Type.Should().NotBeNull();
        result.Location.Should().NotBeNull();
        result.Identifiers.Should().HaveCount(1);
        result.Parties.Should().HaveCount(1);
        result.Species.Should().HaveCount(1);
        result.Marks.Should().HaveCount(1);
        result.Activities.Should().HaveCount(1);
    }

    [Fact]
    public void ToDto_WithMinimalSiteDocument_ShouldHandleNullCollections()
    {
        // Arrange
        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Identifiers = [],
            Parties = [],
            Species = [],
            Marks = [],
            Activities = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("site-123");
        result.Identifiers.Should().BeEmpty();
        result.Parties.Should().BeEmpty();
        result.Species.Should().BeEmpty();
        result.Marks.Should().BeEmpty();
        result.Activities.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WithNullType_ShouldMapTypeAsNull()
    {
        // Arrange
        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Type = null,
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Type.Should().BeNull();
    }

    [Fact]
    public void ToDto_SiteTypeSummaryDocument_ShouldMapCorrectly()
    {
        // Arrange
        var typeDocument = new SiteTypeSummaryDocument
        {
            IdentifierId = "type-1",
            Code = "AH",
            Name = "Agricultural Holding",
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Type = typeDocument,
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Type.Should().NotBeNull();
        result.Type!.IdentifierId.Should().Be("type-1");
        result.Type.Code.Should().Be("AH");
        result.Type.Name.Should().Be("Agricultural Holding");
    }

    [Fact]
    public void ToDto_SiteActivityDocument_ShouldFlattenTypeProperties()
    {
        // Arrange
        var activityDocument = new SiteActivityDocument
        {
            IdentifierId = "activity-1",
            Type = new SiteActivityTypeSummaryDocument()
            {
                IdentifierId = "type-1",
                Code = "LR",
                Name = "Lairage"
            },
            StartDate = new DateTime(2023, 1, 1),
            EndDate = new DateTime(2024, 1, 1)
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Activities = [activityDocument],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Activities.Should().HaveCount(1);
        result.Activities[0].Id.Should().Be("activity-1");
        result.Activities[0].Code.Should().Be("LR");
        result.Activities[0].Name.Should().Be("Lairage");
        result.Activities[0].StartDate.Should().Be(new DateTime(2023, 1, 1));
        result.Activities[0].EndDate.Should().Be(new DateTime(2024, 1, 1));
    }

    [Fact]
    public void ToDto_SiteIdentifierDocument_ShouldMapCorrectly()
    {
        // Arrange
        var identifierDocument = new SiteIdentifierDocument
        {
            IdentifierId = "id-1",
            Identifier = "12/345/6789",
            Type = new SiteIdentifierSummaryDocument
            {
                IdentifierId = "type-1",
                Code = "CPHN",
                Name = "CPH Number",
                LastUpdatedDate = DateTime.UtcNow
            },
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Identifiers = [identifierDocument]
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Identifiers.Should().HaveCount(1);
        result.Identifiers[0].IdentifierId.Should().Be("id-1");
        result.Identifiers[0].Identifier.Should().Be("12/345/6789");
        result.Identifiers[0].Type.Code.Should().Be("CPHN");
        result.Identifiers[0].Type.Name.Should().Be("CPH Number");
    }

    [Fact]
    public void ToDto_LocationDocument_ShouldMapCorrectly()
    {
        // Arrange
        var locationDocument = new LocationDocument
        {
            IdentifierId = "loc-1",
            OsMapReference = "SK123456",
            Easting = 123456,
            Northing = 654321,
            Address = new AddressDocument
            {
                IdentifierId = "addr-1",
                Postcode = "SW1A 1AA",
                PostTown = "London",
                AddressLine1 = "test"
            },
            Communication = [
                new CommunicationDocument
                {
                    IdentifierId = "comm-1",
                    Email = "test@example.com",
                    LastUpdatedDate = DateTime.UtcNow
                }
            ],
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Location = locationDocument,
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Location.Should().NotBeNull();
        result.Location!.OsMapReference.Should().Be("SK123456");
        result.Location.Easting.Should().Be(123456);
        result.Location.Northing.Should().Be(654321);
        result.Location.Address.Should().NotBeNull();
        result.Location.Address!.Postcode.Should().Be("SW1A 1AA");
        result.Location.Communication.Should().HaveCount(1);
        result.Location.Communication[0].Email.Should().Be("test@example.com");
    }

    [Fact]
    public void ToDto_LocationDocument_WithNullCommunication_ShouldReturnEmptyList()
    {
        // Arrange
        var locationDocument = new LocationDocument
        {
            IdentifierId = "loc-1",
            Communication = [],
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Location = locationDocument,
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Location.Should().NotBeNull();
        result.Location!.Communication.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_AddressDocument_ShouldMapCorrectly()
    {
        // Arrange
        var addressDocument = new AddressDocument
        {
            IdentifierId = "addr-1",
            Uprn = "10002345",
            AddressLine1 = "10 Downing Street",
            AddressLine2 = "Westminster",
            PostTown = "London",
            County = "Greater London",
            Postcode = "SW1A 2AA",
            Country = new CountrySummaryDocument
            {
                IdentifierId = "country-1",
                Code = "GB-ENG",
                Name = "England",
                LongName = "England - United Kingdom",
                EuTradeMemberFlag = false,
                DevolvedAuthorityFlag = true,
                LastModifiedDate = DateTime.UtcNow
            },
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Location = new LocationDocument
            {
                IdentifierId = "loc-1",
                Address = addressDocument,
                LastUpdatedDate = DateTime.UtcNow
            },
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Location!.Address.Should().NotBeNull();
        var address = result.Location.Address!;
        address.Uprn.Should().Be("10002345");
        address.AddressLine1.Should().Be("10 Downing Street");
        address.Postcode.Should().Be("SW1A 2AA");
        address.Country.Should().NotBeNull();
        address.Country!.Code.Should().Be("GB-ENG");
        address.Country.Name.Should().Be("England");
    }

    [Fact]
    public void ToDto_SitePartyDocument_ShouldMapCorrectly()
    {
        // Arrange
        var partyDocument = new SitePartyDocument
        {
            IdentifierId = "party-1",
            CustomerNumber = "C12345",
            Title = "Mr",
            FirstName = "John",
            LastName = "Doe",
            Name = "John Doe",
            PartyType = "Person",
            State = "Active",
            LastUpdatedDate = DateTime.UtcNow,
            Communication = [
                new CommunicationDocument
                {
                    IdentifierId = "comm-1",
                    Email = "john@example.com",
                    Mobile = "07700900000",
                    LastUpdatedDate = DateTime.UtcNow
                }
            ],
            CorrespondanceAddress = new AddressDocument
            {
                AddressLine1 = "testAdd",
                IdentifierId = "addr-1",
                Postcode = "AB12 3CD"
            },
            PartyRoles = [
                new PartyRoleDocument
                {
                    IdentifierId = "role-1",
                    Role = new PartyRoleRoleDocument
                    {
                        IdentifierId = "role-def-1",
                        Code = "OWNER",
                        Name = "Livestock Owner",
                        LastUpdatedDate = DateTime.UtcNow
                    },
                    SpeciesManagedByRole = [],
                    LastUpdatedDate = DateTime.UtcNow
                }
            ]
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Parties = [partyDocument],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Parties.Should().HaveCount(1);
        var party = result.Parties[0];
        party.CustomerNumber.Should().Be("C12345");
        party.FirstName.Should().Be("John");
        party.LastName.Should().Be("Doe");
        party.Communication.Should().HaveCount(1);
        party.Communication[0].Email.Should().Be("john@example.com");
        party.PartyRoles.Should().HaveCount(1);
        party.PartyRoles[0].Role.Code.Should().Be("OWNER");
    }

    [Fact]
    public void ToDto_SitePartyDocument_WithNullCollections_ShouldReturnEmptyLists()
    {
        // Arrange
        var partyDocument = new SitePartyDocument
        {
            IdentifierId = "party-1",
            CustomerNumber = "C12345",
            Name = "Test Party",
            LastUpdatedDate = DateTime.UtcNow,
            Communication = [],
            PartyRoles = []
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Parties = [partyDocument],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Parties[0].Communication.Should().BeEmpty();
        result.Parties[0].PartyRoles.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_PartyRoleDocument_ShouldMapCorrectly()
    {
        // Arrange
        var partyRoleDocument = new PartyRoleDocument
        {
            IdentifierId = "role-1",
            Role = new PartyRoleRoleDocument
            {
                IdentifierId = "role-def-1",
                Code = "KEEPER",
                Name = "Livestock Keeper",
                LastUpdatedDate = DateTime.UtcNow
            },
            SpeciesManagedByRole = [
                new ManagedSpeciesDocument
                {
                    IdentifierId = "species-1",
                    Code = "CTT",
                    Name = "Cattle",
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = null,
                    LastUpdatedDate = DateTime.UtcNow
                }
            ],
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Parties = [
                new SitePartyDocument
                {
                    IdentifierId = "party-1",
                    CustomerNumber = "C12345",
                    Name = "Test Party",
                    LastUpdatedDate = DateTime.UtcNow,
                    PartyRoles = [partyRoleDocument]
                }
            ],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        var role = result.Parties[0].PartyRoles[0];
        role.Role.Code.Should().Be("KEEPER");
        role.Role.Name.Should().Be("Livestock Keeper");
        role.SpeciesManagedByRole.Should().HaveCount(1);
        role.SpeciesManagedByRole[0].Code.Should().Be("CTT");
        role.SpeciesManagedByRole[0].Name.Should().Be("Cattle");
        role.SpeciesManagedByRole[0].EndDate.Should().BeNull();
    }

    [Fact]
    public void ToDto_PartyRoleDocument_WithNullSpecies_ShouldReturnEmptyList()
    {
        // Arrange
        var partyRoleDocument = new PartyRoleDocument
        {
            IdentifierId = "role-1",
            Role = new PartyRoleRoleDocument
            {
                IdentifierId = "role-def-1",
                Code = "OWNER",
                Name = "Owner",
                LastUpdatedDate = DateTime.UtcNow
            },
            SpeciesManagedByRole = [],
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Parties = [
                new SitePartyDocument
                {
                    IdentifierId = "party-1",
                    CustomerNumber = "C12345",
                    Name = "Test",
                    LastUpdatedDate = DateTime.UtcNow,
                    PartyRoles = [partyRoleDocument]
                }
            ],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Parties[0].PartyRoles[0].SpeciesManagedByRole.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_SpeciesSummaryDocument_ShouldMapCorrectly()
    {
        // Arrange
        var speciesDocument = new SpeciesSummaryDocument
        {
            IdentifierId = "species-1",
            Code = "SHP",
            Name = "Sheep",
            LastModifiedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Species = [speciesDocument],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Species.Should().HaveCount(1);
        result.Species[0].Code.Should().Be("SHP");
        result.Species[0].Name.Should().Be("Sheep");
    }

    [Fact]
    public void ToDto_GroupMarkDocument_ShouldMapCorrectly()
    {
        // Arrange
        var groupMarkDocument = new GroupMarkDocument
        {
            IdentifierId = "mark-1",
            Mark = "H12345",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2024, 1, 1),
            Species = [
                new SpeciesSummaryDocument
                {
                    IdentifierId = "species-1",
                    Code = "CTT",
                    Name = "Cattle",
                    LastModifiedDate = DateTime.UtcNow
                }
            ],
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Marks = [groupMarkDocument],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Marks.Should().HaveCount(1);
        result.Marks[0].Mark.Should().Be("H12345");
        result.Marks[0].Species.Should().HaveCount(1);
        result.Marks[0].Species[0].Code.Should().Be("CTT");
    }

    [Fact]
    public void ToDto_GroupMarkDocument_WithNullSpecies_ShouldReturnEmptyList()
    {
        // Arrange
        var groupMarkDocument = new GroupMarkDocument
        {
            IdentifierId = "mark-1",
            Mark = "H12345",
            StartDate = DateTime.UtcNow,
            Species = [],
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Marks = [groupMarkDocument],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        result.Marks[0].Species.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_CommunicationDocument_ShouldMapCorrectly()
    {
        // Arrange
        var communicationDocument = new CommunicationDocument
        {
            IdentifierId = "comm-1",
            Email = "test@example.com",
            Mobile = "07700900000",
            Landline = "01234567890",
            PrimaryContactFlag = true,
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Location = new LocationDocument
            {
                IdentifierId = "loc-1",
                Communication = [communicationDocument],
                LastUpdatedDate = DateTime.UtcNow
            },
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        var comm = result.Location!.Communication[0];
        comm.Email.Should().Be("test@example.com");
        comm.Mobile.Should().Be("07700900000");
        comm.Landline.Should().Be("01234567890");
        comm.PrimaryContactFlag.Should().BeTrue();
    }

    [Fact]
    public void ToDto_ManagedSpeciesDocument_ShouldMapCorrectly()
    {
        // Arrange
        var managedSpeciesDocument = new ManagedSpeciesDocument
        {
            IdentifierId = "species-1",
            Code = "PIG",
            Name = "Pigs",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            LastUpdatedDate = DateTime.UtcNow
        };

        var siteDocument = new SiteDocument
        {
            Id = "site-123",
            Name = "Test Site",
            StartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Parties = [
                new SitePartyDocument
                {
                    IdentifierId = "party-1",
                    CustomerNumber = "C12345",
                    Name = "Test",
                    LastUpdatedDate = DateTime.UtcNow,
                    PartyRoles = [
                        new PartyRoleDocument
                        {
                            IdentifierId = "role-1",
                            Role = new PartyRoleRoleDocument
                            {
                                IdentifierId = "role-def-1",
                                Code = "OWNER",
                                Name = "Owner",
                                LastUpdatedDate = DateTime.UtcNow
                            },
                            SpeciesManagedByRole = [managedSpeciesDocument],
                            LastUpdatedDate = DateTime.UtcNow
                        }
                    ]
                }
            ],
            Identifiers = []
        };

        // Act
        var result = siteDocument.ToDto();

        // Assert
        var species = result.Parties[0].PartyRoles[0].SpeciesManagedByRole[0];
        species.Code.Should().Be("PIG");
        species.Name.Should().Be("Pigs");
        species.StartDate.Should().Be(new DateTime(2020, 1, 1));
        species.EndDate.Should().Be(new DateTime(2024, 12, 31));
    }

    private static SiteDocument CreateFullSiteDocument()
    {
        return new SiteDocument
        {
            Id = "site-123",
            LastUpdatedDate = DateTime.UtcNow,
            Name = "Test Farm",
            State = "Active",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = null,
            Source = "SAM",
            DestroyIdentityDocumentsFlag = false,
            Type = new SiteTypeSummaryDocument
            {
                IdentifierId = "type-1",
                Code = "AH",
                Name = "Agricultural Holding",
                LastUpdatedDate = DateTime.UtcNow
            },
            Location = new LocationDocument
            {
                IdentifierId = "loc-1",
                OsMapReference = "SK123456",
                LastUpdatedDate = DateTime.UtcNow
            },
            Identifiers = [
                new SiteIdentifierDocument
                {
                    IdentifierId = "id-1",
                    Identifier = "12/345/6789",
                    Type = new SiteIdentifierSummaryDocument
                    {
                        IdentifierId = "type-1",
                        Code = "CPHN",
                        Name = "CPH Number",
                        LastUpdatedDate = DateTime.UtcNow
                    },
                    LastUpdatedDate = DateTime.UtcNow
                }
            ],
            Parties = [
                new SitePartyDocument
                {
                    IdentifierId = "party-1",
                    CustomerNumber = "C12345",
                    Name = "Test Party",
                    LastUpdatedDate = DateTime.UtcNow
                }
            ],
            Species = [
                new SpeciesSummaryDocument
                {
                    IdentifierId = "species-1",
                    Code = "CTT",
                    Name = "Cattle",
                    LastModifiedDate = DateTime.UtcNow
                }
            ],
            Marks = [
                new GroupMarkDocument
                {
                    IdentifierId = "mark-1",
                    Mark = "H12345",
                    StartDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    Species = []
                }
            ],
            Activities = [
                new SiteActivityDocument
                {
                    IdentifierId = "activity-1",
                    Type = new SiteActivityTypeSummaryDocument
                    {
                        IdentifierId = "type-1",
                        Code = "LR",
                        Name = "Lairage"
                    },
                    StartDate = DateTime.UtcNow
                }
            ]
        };
    }
}