using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;

namespace KeeperData.Core.Tests.Unit.Documents;

public class PartyDocumentExtensionsTests
{
    [Fact]
    public void ToDto_WithFullPartyDocument_ShouldMapAllProperties()
    {
        // Arrange
        var partyDocument = CreateFullPartyDocument();

        // Act
        var result = partyDocument.ToDto();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(partyDocument.Id);
        result.LastUpdatedDate.Should().Be(partyDocument.LastUpdatedDate);
        result.Title.Should().Be(partyDocument.Title);
        result.FirstName.Should().Be(partyDocument.FirstName);
        result.LastName.Should().Be(partyDocument.LastName);
        result.Name.Should().Be(partyDocument.Name);
        result.CustomerNumber.Should().Be(partyDocument.CustomerNumber);
        result.PartyType.Should().Be(partyDocument.PartyType);
        result.State.Should().Be(partyDocument.State);
        result.Communication.Should().HaveCount(1);
        result.CorrespondenceAddress.Should().NotBeNull();
        result.PartyRoles.Should().HaveCount(1);
    }

    [Fact]
    public void ToDto_WithMinimalPartyDocument_ShouldHandleEmptyCollections()
    {
        // Arrange
        var partyDocument = new PartyDocument
        {
            Id = "party-123",
            Communication = [],
            PartyRoles = []
        };

        // Act
        var result = partyDocument.ToDto();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("party-123");
        result.Communication.Should().BeEmpty();
        result.CorrespondenceAddress.Should().BeNull();
        result.PartyRoles.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_ShouldExcludeCreatedDateAndDeleted()
    {
        // Arrange
        var partyDocument = new PartyDocument
        {
            Id = "party-123",
            CreatedDate = new DateTime(2023, 1, 1),
            Deleted = true
        };

        // Act
        var result = partyDocument.ToDto();

        // Assert
        result.Should().NotBeNull();
        result.GetType().GetProperty("CreatedDate").Should().BeNull();
        result.GetType().GetProperty("Deleted").Should().BeNull();
    }

    [Fact]
    public void ToDto_ShouldMapCorrespondanceAddressToCorrespondenceAddress()
    {
        // Arrange
        var partyDocument = new PartyDocument
        {
            Id = "party-123",
            CorrespondanceAddress = new AddressDocument
            {
                IdentifierId = "addr-1",
                AddressLine1 = "10 Downing Street",
                Postcode = "SW1A 2AA",
                LastUpdatedDate = DateTime.UtcNow
            }
        };

        // Act
        var result = partyDocument.ToDto();

        // Assert
        result.CorrespondenceAddress.Should().NotBeNull();
        result.CorrespondenceAddress!.IdentifierId.Should().Be("addr-1");
        result.CorrespondenceAddress.AddressLine1.Should().Be("10 Downing Street");
        result.CorrespondenceAddress.Postcode.Should().Be("SW1A 2AA");
    }

    [Fact]
    public void ToDto_WithNullCorrespondanceAddress_ShouldMapAsNull()
    {
        // Arrange
        var partyDocument = new PartyDocument
        {
            Id = "party-123",
            CorrespondanceAddress = null
        };

        // Act
        var result = partyDocument.ToDto();

        // Assert
        result.CorrespondenceAddress.Should().BeNull();
    }

    [Fact]
    public void ToDto_PartyRoleWithSiteDocument_ShouldMapCorrectly()
    {
        // Arrange
        var roleDocument = new PartyRoleWithSiteDocument
        {
            IdentifierId = "role-1",
            Site = new PartyRoleSiteDocument
            {
                IdentifierId = "site-1",
                Name = "Test Farm",
                State = "Active",
                Identifiers = [],
                LastUpdatedDate = DateTime.UtcNow
            },
            Role = new PartyRoleRoleDocument
            {
                IdentifierId = "role-ref-1",
                Code = "LIVESTOCKKEEPER",
                Name = "Livestock Keeper",
                LastUpdatedDate = DateTime.UtcNow
            },
            SpeciesManagedByRole =
            [
                new ManagedSpeciesDocument
                {
                    IdentifierId = "species-1",
                    Code = "CTT",
                    Name = "Cattle",
                    StartDate = new DateTime(2023, 1, 1),
                    EndDate = null,
                    LastUpdatedDate = DateTime.UtcNow
                }
            ],
            LastUpdatedDate = DateTime.UtcNow
        };

        var partyDocument = new PartyDocument
        {
            Id = "party-123",
            PartyRoles = [roleDocument]
        };

        // Act
        var result = partyDocument.ToDto();

        // Assert
        result.PartyRoles.Should().HaveCount(1);
        var role = result.PartyRoles[0];
        role.IdentifierId.Should().Be("role-1");
        role.Site.Should().NotBeNull();
        role.Site!.IdentifierId.Should().Be("site-1");
        role.Site.Name.Should().Be("Test Farm");
        role.Site.State.Should().Be("Active");
        role.Role.IdentifierId.Should().Be("role-ref-1");
        role.Role.Code.Should().Be("LIVESTOCKKEEPER");
        role.Role.Name.Should().Be("Livestock Keeper");
        role.SpeciesManagedByRole.Should().HaveCount(1);
        role.SpeciesManagedByRole[0].Code.Should().Be("CTT");
        role.SpeciesManagedByRole[0].Name.Should().Be("Cattle");
    }

    [Fact]
    public void ToDto_PartyRoleSiteDocument_ShouldMapCorrectly()
    {
        // Arrange
        var siteDocument = new PartyRoleSiteDocument
        {
            IdentifierId = "site-1",
            Name = "Test Farm",
            Type = new SiteTypeSummaryDocument
            {
                IdentifierId = "type-1",
                Code = "AH",
                Name = "Agricultural Holding",
                LastUpdatedDate = DateTime.UtcNow
            },
            State = "Active",
            Identifiers =
            [
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
            LastUpdatedDate = DateTime.UtcNow
        };

        var partyDocument = new PartyDocument
        {
            Id = "party-123",
            PartyRoles =
            [
                new PartyRoleWithSiteDocument
                {
                    IdentifierId = "role-1",
                    Site = siteDocument,
                    Role = new PartyRoleRoleDocument
                    {
                        IdentifierId = "role-ref-1",
                        Code = "LIVESTOCKKEEPER",
                        Name = "Livestock Keeper"
                    }
                }
            ]
        };

        // Act
        var result = partyDocument.ToDto();

        // Assert
        var site = result.PartyRoles[0].Site;
        site.Should().NotBeNull();
        site!.IdentifierId.Should().Be("site-1");
        site.Name.Should().Be("Test Farm");
        site.Type.Should().NotBeNull();
        site.Type!.Code.Should().Be("AH");
        site.Type.Name.Should().Be("Agricultural Holding");
        site.State.Should().Be("Active");
        site.Identifiers.Should().HaveCount(1);
        site.Identifiers[0].Identifier.Should().Be("12/345/6789");
        site.Identifiers[0].Type.Code.Should().Be("CPHN");
    }

    [Fact]
    public void ToDto_CommunicationDocument_ShouldMapCorrectly()
    {
        // Arrange
        var partyDocument = new PartyDocument
        {
            Id = "party-123",
            Communication =
            [
                new CommunicationDocument
                {
                    IdentifierId = "comm-1",
                    Email = "john@example.com",
                    Mobile = "07123456789",
                    Landline = "0114 1234567",
                    PrimaryContactFlag = true,
                    LastUpdatedDate = DateTime.UtcNow
                }
            ]
        };

        // Act
        var result = partyDocument.ToDto();

        // Assert
        result.Communication.Should().HaveCount(1);
        var comm = result.Communication[0];
        comm.IdentifierId.Should().Be("comm-1");
        comm.Email.Should().Be("john@example.com");
        comm.Mobile.Should().Be("07123456789");
        comm.Landline.Should().Be("0114 1234567");
        comm.PrimaryContactFlag.Should().BeTrue();
    }

    [Fact]
    public void ToDto_AddressDocument_ShouldMapCorrectly()
    {
        // Arrange
        var partyDocument = new PartyDocument
        {
            Id = "party-123",
            CorrespondanceAddress = new AddressDocument
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
            }
        };

        // Act
        var result = partyDocument.ToDto();

        // Assert
        result.CorrespondenceAddress.Should().NotBeNull();
        var addr = result.CorrespondenceAddress!;
        addr.IdentifierId.Should().Be("addr-1");
        addr.Uprn.Should().Be("10002345");
        addr.AddressLine1.Should().Be("10 Downing Street");
        addr.AddressLine2.Should().Be("Westminster");
        addr.PostTown.Should().Be("London");
        addr.County.Should().Be("Greater London");
        addr.Postcode.Should().Be("SW1A 2AA");
        addr.Country.Should().NotBeNull();
        addr.Country!.Code.Should().Be("GB-ENG");
        addr.Country.Name.Should().Be("England");
        addr.Country.LongName.Should().Be("England - United Kingdom");
        addr.Country.EuTradeMemberFlag.Should().BeFalse();
        addr.Country.DevolvedAuthorityFlag.Should().BeTrue();
    }

    private static PartyDocument CreateFullPartyDocument()
    {
        return new PartyDocument
        {
            Id = "party-full",
            CreatedDate = new DateTime(2023, 1, 1),
            LastUpdatedDate = new DateTime(2024, 1, 1),
            Title = "Mr",
            FirstName = "John",
            LastName = "Doe",
            Name = "John Doe",
            CustomerNumber = "C77473",
            PartyType = "Person",
            State = "Active",
            Deleted = false,
            Communication =
            [
                new CommunicationDocument
                {
                    IdentifierId = "comm-1",
                    Email = "john.doe@example.com",
                    Mobile = "07123456789",
                    Landline = "0114 1234567",
                    PrimaryContactFlag = true,
                    LastUpdatedDate = new DateTime(2024, 1, 1)
                }
            ],
            CorrespondanceAddress = new AddressDocument
            {
                IdentifierId = "addr-1",
                Uprn = "10002345",
                AddressLine1 = "Test Farm, Farm Lane",
                AddressLine2 = "Cloverfield",
                PostTown = "Sheffield",
                County = "South Yorkshire",
                Postcode = "S36 2BS",
                Country = new CountrySummaryDocument
                {
                    IdentifierId = "country-1",
                    Code = "GB-ENG",
                    Name = "England",
                    LongName = "England - United Kingdom",
                    EuTradeMemberFlag = false,
                    DevolvedAuthorityFlag = true,
                    LastModifiedDate = new DateTime(2024, 1, 1)
                },
                LastUpdatedDate = new DateTime(2024, 1, 1)
            },
            PartyRoles =
            [
                new PartyRoleWithSiteDocument
                {
                    IdentifierId = "role-1",
                    Site = new PartyRoleSiteDocument
                    {
                        IdentifierId = "site-1",
                        Name = "Test Farm",
                        Type = new SiteTypeSummaryDocument
                        {
                            IdentifierId = "type-1",
                            Code = "AH",
                            Name = "Agricultural Holding",
                            LastUpdatedDate = new DateTime(2024, 1, 1)
                        },
                        State = "Active",
                        Identifiers =
                        [
                            new SiteIdentifierDocument
                            {
                                IdentifierId = "id-1",
                                Identifier = "12/345/6789",
                                Type = new SiteIdentifierSummaryDocument
                                {
                                    IdentifierId = "idtype-1",
                                    Code = "CPHN",
                                    Name = "CPH Number",
                                    LastUpdatedDate = new DateTime(2024, 1, 1)
                                },
                                LastUpdatedDate = new DateTime(2024, 1, 1)
                            }
                        ],
                        LastUpdatedDate = new DateTime(2024, 1, 1)
                    },
                    Role = new PartyRoleRoleDocument
                    {
                        IdentifierId = "role-ref-1",
                        Code = "LIVESTOCKKEEPER",
                        Name = "Livestock Keeper",
                        LastUpdatedDate = new DateTime(2024, 1, 1)
                    },
                    SpeciesManagedByRole =
                    [
                        new ManagedSpeciesDocument
                        {
                            IdentifierId = "species-1",
                            Code = "CTT",
                            Name = "Cattle",
                            StartDate = new DateTime(2023, 1, 1),
                            EndDate = null,
                            LastUpdatedDate = new DateTime(2024, 1, 1)
                        }
                    ],
                    LastUpdatedDate = new DateTime(2024, 1, 1)
                }
            ]
        };
    }
}
