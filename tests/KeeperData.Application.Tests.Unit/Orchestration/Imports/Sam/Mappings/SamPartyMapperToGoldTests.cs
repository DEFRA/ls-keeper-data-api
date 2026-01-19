using System.Diagnostics;
using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Moq;
using CommunicationDocument = KeeperData.Core.Documents.Silver.CommunicationDocument;
using PartyRoleDocument = KeeperData.Core.Documents.Silver.PartyRoleDocument;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamPartyMapperToGoldTests
{
    private Func<string?, CancellationToken, Task<CountryDocument?>> _getCountryById;
    private Func<string?, CancellationToken, Task<SpeciesDocument?>> _getSpeciesById;
    private Mock<IPartiesRepository> _goldRepoMock;
    const string GoldSiteId = "gold-site-id";

    private List<CountryDocument> _countryData = [
        new CountryDocument { IdentifierId = "en123", Code = "GB-ENG", Name = "England", LongName = "England - United Kingdom"},
        new CountryDocument { IdentifierId = "fr123", Code = "FR", Name = "France", LongName = "French Republic"},
        new CountryDocument { IdentifierId = "nz123", Code = "NZ", Name = "New Zealand", LongName = "New Zealand"}];

    public SamPartyMapperToGoldTests()
    {
        _goldRepoMock = new Mock<IPartiesRepository>();
        var species = new SpeciesDocument() { IdentifierId = "p123", Code = "P", Name = "Pig" };
        _getCountryById = (string? key, CancellationToken token) => Task.FromResult(_countryData.SingleOrDefault(x => x.IdentifierId == key));
        _getSpeciesById = (string? key, CancellationToken token) => Task.FromResult<SpeciesDocument?>(species);
    }

    public static IEnumerable<object[]> TestDataForNewGoldMappings
    {
        get
        {
            yield return ["When mapping new null PartyDocument", (SamPartyDocument s) => { }, (PartyDocument d) => { }];
            yield return ["When mapping deleted PartyDocument",
                (SamPartyDocument s) => { s.Deleted = true; },
                (PartyDocument d) => {
                    d.State = "inactive";
                    d.Deleted = true;
                }];
            yield return ["When mapping PartyDocument core Properties",
                (SamPartyDocument input) => {
                    input.CountyParishHoldingNumber = "12";         // ignored
                    input.CphList = new List<string> { "a", "b" };  // ignored
                    input.LastUpdatedBatchId = 13;                  // ignored
                    input.PartyFirstName = "Joe";
                    input.PartyLastName = "Smith";
                    input.PartyFullName = "Joseph M Smith";
                    input.PartyInitials = "JMS";                    // ignored
                    input.PartyTitleTypeIdentifier = "ptti";
                },
                (PartyDocument expected) =>
                {
                    expected.Title = "ptti";
                    expected.FirstName = "Joe";
                    expected.LastName = "Smith";
                    expected.Name = "Joseph M Smith";
                }];
            yield return ["When mapping empty PartyDocument.Address",
                (SamPartyDocument input) => {
                    input.Address = new Core.Documents.Silver.AddressDocument
                    {
                        IdentifierId = "some-guid",
                    };
                },
                (PartyDocument expected) => { /* no change */ }];
            yield return ["When mapping empty PartyDocument.Address with country that does not exist",
                (SamPartyDocument input) => {
                    input.Address = new Core.Documents.Silver.AddressDocument
                    {
                        IdentifierId = "some-guid",
                        CountryIdentifier = "invalid-id"
                    };
                },
                (PartyDocument expected) => { /* no change */ }];
            yield return ["When mapping empty PartyDocument.Address with country that does exist",
                (SamPartyDocument input) => {
                    input.Address = new Core.Documents.Silver.AddressDocument
                        { IdentifierId = "some-guid", CountryIdentifier = "fr123" };
                },
                (PartyDocument expected) => {
                    expected.CorrespondanceAddress!.Country = new CountrySummaryDocument
                        { IdentifierId = "fr123", Code = "FR", Name = "France", LongName = "French Republic" };
                }];
            yield return [ "When mapping PartyDocument.Address without country id",
                (SamPartyDocument input) => {
                    input.Address = new Core.Documents.Silver.AddressDocument
                    {
                        IdentifierId = "some-guid",
                        AddressLine = "line",
                        AddressLocality = "locale",
                        AddressPostCode = "postcode",
                        AddressStreet = "street",
                        AddressTown = "town",
                        CountryCode = "countryCode",            // ignored
                        CountrySubDivision = "country-subdiv",  // ignored
                        UniquePropertyReferenceNumber = "1234"
                    };
                },
                (PartyDocument expected) =>
                {
                    expected.CorrespondanceAddress!.AddressLine1 = "line";
                    expected.CorrespondanceAddress!.AddressLine2 = "street";
                    expected.CorrespondanceAddress!.County = "locale";
                    expected.CorrespondanceAddress!.Postcode = "postcode";
                    expected.CorrespondanceAddress!.PostTown = "town";
                    expected.CorrespondanceAddress!.Uprn = 1234;
                }];
            yield return [ "When mapping empty PartyDocument.Communication",
                (SamPartyDocument input) =>
                {
                    input.Communication = new CommunicationDocument()
                    {
                        IdentifierId = "some-guid"
                    };
                },
                (PartyDocument expected) =>
                {
                    expected.Communication =
                        [new Core.Documents.CommunicationDocument { IdentifierId = "some-guid", Email = null, Landline = null, Mobile = null, PrimaryContactFlag = false }];
                }];
            yield return [ "When mapping PartyDocument.Communication",
                (SamPartyDocument input) =>
                {
                    input.Communication = new CommunicationDocument()
                    {
                        IdentifierId = "some-guid", Email = "email", Landline = "landline", Mobile = "mobile"
                    };
                },
                (PartyDocument expected) =>
                {
                    expected.Communication =
                        [new Core.Documents.CommunicationDocument { IdentifierId = "some-guid", Email = "email", Landline = "landline", Mobile = "mobile", PrimaryContactFlag = false }];
                }];
            yield return [ "When mapping empty PartyDocument.Roles",
                (SamPartyDocument input) =>
                {
                    input.Roles = new List<PartyRoleDocument>() { };
                },
                (PartyDocument expected) =>
                {
                    /*no change*/
                }
            ];
            yield return [ "When mapping null PartyDocument.Roles",
                (SamPartyDocument input) =>
                {
                    input.Roles = null;
                },
                (PartyDocument expected) =>
                {
                    /*no change*/
                }
            ];
            yield return [ "When mapping PartyDocument.Roles",
                (SamPartyDocument input) =>
                {
                    input.Roles = new List<PartyRoleDocument>()
                    {
                        new()
                        {
                            IdentifierId = "prd-id",
                            RoleTypeId = "role-a",
                            RoleTypeName = "role-name",
                            RoleTypeCode = "role-code",
                            EffectiveFromDate = new DateTime(2001,01,01), // not used
                            EffectiveToDate = new DateTime(2002,01,01), // not used
                            SourceRoleName = "source-name" // not used
                        }
                    };
                },
                (PartyDocument expected) =>
                {
                    expected.PartyRoles = new List<PartyRoleWithSiteDocument>
                    {
                        new()
                        {
                            IdentifierId = "any-guid",
                            Role = new PartyRoleRoleDocument { IdentifierId = "role-a", Code = "role-code", Name = "role-name" },
                            Site = new PartyRoleSiteDocument
                            {
                                IdentifierId = GoldSiteId // TODO seems odd - elsewhere identifierid is a primary key; this looks to be a foreign key
                                // lastupdated date is set to now
                                // the rest are null
                            },
                        }
                    };
                }
            ];

        }
    }

    [Theory]
    [MemberData(nameof(TestDataForNewGoldMappings))]
    public async Task ToGoldShouldCreatePartyWithCorrectMapping(string testName, Action<SamPartyDocument> modifyInput, Action<PartyDocument> modifyExpected)
    {
        var inputParty = new SamPartyDocument();
        var expected = CreateNewEmptyPartyDocument();

        Debug.WriteLine($"in testcase {testName}");
        modifyInput(inputParty);
        modifyExpected(expected);

        var result = await WhenIMapNewPartyToGold(inputParty);

        WipeIdsAndLastUpdatedDates(result);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ToGoldShouldNotModifyCollectionOfExistingPartyIdsWhenPartyIsNew()
    {
        var existingPartyIds = new List<string> { "gold-id-a", "gold-id-b" };
        var inputParty = new SamPartyDocument();
        inputParty.PartyId = "customer-number";

        var result = await WhenIMapNewPartyToGold(inputParty, existingPartyIds: existingPartyIds);
        var expectedPartyIds = new List<string>() { "gold-id-a", "gold-id-b" };

        result.CustomerNumber.Should().Be("customer-number");
        existingPartyIds.Should().BeEquivalentTo(expectedPartyIds);
    }

    [Fact]
    public async Task ToGoldShouldModifyCollectionOfExistingPartyIdsWhenPartyIdExists()
    {
        var existingPartyIds = new List<string> { "gold-id-a" };
        var existingCustomerNumber = "existing-customer-number";
        var inputParty = new SamPartyDocument();
        inputParty.PartyId = existingCustomerNumber;
        var existingParty = new PartyDocument() { Id = "gold-id", CustomerNumber = existingCustomerNumber };
        var expectedPartyIds = new List<string> { "gold-id-a", "gold-id" };
        _goldRepoMock
            .Setup(r => r.FindPartyByCustomerNumber(existingCustomerNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParty);

        var result = await WhenIMapNewPartyToGold(inputParty, existingPartyIds: existingPartyIds);

        result.CustomerNumber.Should().Be(existingCustomerNumber);
        existingPartyIds.Should().BeEquivalentTo(expectedPartyIds);
    }

    [Theory]
    [MemberData(nameof(TestDataForNewGoldMappings))]
    public async Task ToGoldShouldUpdatePartyWithCorrectMapping(string testName, Action<SamPartyDocument> modifyInput, Action<PartyDocument> modifyExpected)
    {
        var existingId = "existing-id";
        var inputParty = new SamPartyDocument();
        inputParty.PartyId = existingId;
        var existingParty = new PartyDocument() { Id = "gold-id", CustomerNumber = existingId };
        _goldRepoMock
            .Setup(r => r.FindPartyByCustomerNumber(existingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParty);

        var expected = CreateNewEmptyPartyDocument();

        Debug.WriteLine($"in testcase {testName}");
        modifyInput(inputParty);
        modifyExpected(expected);

        expected.CustomerNumber = existingId;

        var result = await WhenIMapNewPartyToGold(inputParty, existingPartyIds: new List<string> { "gold-id" });

        WipeIdsAndLastUpdatedDates(result);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    // TODO
    // most we can say is role isn't added as it is already in list;
    // but there are comparisons that look like they are attempting to update properties like .Code
    // could this be a defect?
    [Fact]
    public async Task ToGoldShouldNotUpdateCodeOrNameOfRolesAlreadyMappedToParty()
    {
        var customerId = "customer-id";
        var matchingRoleId = "roleMatchId";
        var inputParty = new SamPartyDocument
        {
            PartyId = customerId,
            Roles = new List<PartyRoleDocument>
            {
                new ()
                {
                    IdentifierId = "any-guid",
                    RoleTypeId = matchingRoleId,
                    RoleTypeCode = "newtypecode",
                    RoleTypeName = "newtypename",
                    SourceRoleName = "newsourcerolename"
                }
            }
        };
        inputParty.PartyId = customerId;
        var existingParty = new PartyDocument
        {
            Id = "gold-id",
            CustomerNumber = customerId,
            PartyRoles = new List<PartyRoleWithSiteDocument>
            {
                new()
                {
                    IdentifierId = "abc",
                    Role = new() { IdentifierId = matchingRoleId, Code = "oldcode", Name = "oldname" },
                    Site = new()
                    {
                        IdentifierId = GoldSiteId,
                        Name = "oldsitename",
                        State = "oldstate",
                        Type = new PremisesTypeSummaryDocument { IdentifierId = "any-ptsd-id", Code = "ptsdcode", Description = "ptsd-desc" }
                    }
                }
            }
        };

        var expected = CreateNewEmptyPartyDocument();
        expected.Id = "gold-id";
        expected.CustomerNumber = customerId;
        expected.PartyRoles = new List<PartyRoleWithSiteDocument>()
        {
            new PartyRoleWithSiteDocument()
                { IdentifierId = "abc", // id unchanged,
                    Role = new PartyRoleRoleDocument()
                    {
                        IdentifierId = matchingRoleId,
                        Name = "oldname", //unchanged - see next test - if this were a new record this would be set to the incoming name
                        Code = "oldcode", //unchanged - see next test - if this were a new record this would be set to the incoming code
                    },
                    Site = new PartyRoleSiteDocument()
                    {
                        IdentifierId = GoldSiteId,
                        Name = "oldsitename",
                        State = "oldstate",
                        Type = new PremisesTypeSummaryDocument { IdentifierId = "any-ptsd-id", Code = "ptsdcode", Description = "ptsd-desc" }
                    }
                }
        };

        _goldRepoMock
            .Setup(r => r.FindPartyByCustomerNumber(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParty);

        var result = await WhenIMapNewPartyToGold(inputParty, existingPartyIds: new List<string> { customerId });

        WipeIdsAndLastUpdatedDates(result);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    // TODO 
    // this test seems to suggest that
    // if a role is no longer in the source, it isn't removed
    [Fact]
    public async Task ToGoldShouldAddNewRoleAlongsideExistingRoles()
    {
        var customerId = "customer-id";
        var newRoleTypeId = "new-roleId";
        var existingRoleTypeId = "existing-roleId";
        var inputParty = new SamPartyDocument()
        {
            PartyId = customerId,
            Roles = new List<PartyRoleDocument>
            {
                new PartyRoleDocument()
                {
                    IdentifierId = "any-guid",
                    RoleTypeId = newRoleTypeId,
                    RoleTypeCode = "newtypecode",
                    RoleTypeName = "newtypename",
                    SourceRoleName = "newsourcerolename",
                }
            }
        };

        inputParty.PartyId = customerId;
        var existingParty = new PartyDocument()
        {
            Id = "gold-id",
            CustomerNumber = customerId,
            PartyRoles = new List<PartyRoleWithSiteDocument>() { new PartyRoleWithSiteDocument()
            {
                IdentifierId = "old-role-id",
                Role = new PartyRoleRoleDocument() { IdentifierId = existingRoleTypeId, Code = "oldcode", Name = "oldname",},
                Site = new PartyRoleSiteDocument() { IdentifierId = GoldSiteId, Name = "oldsitename", State = "oldstate"
                , Type = new PremisesTypeSummaryDocument { IdentifierId = "any-ptsd-id", Code = "ptsdcode", Description = "ptsd-desc" } }
            } }

        };

        var expected = CreateNewEmptyPartyDocument();
        expected.Id = "gold-id";
        expected.CustomerNumber = customerId;
        expected.PartyRoles = new List<PartyRoleWithSiteDocument>()
        {
            new ()
                { IdentifierId = "old-role-id",
                    Role = new PartyRoleRoleDocument()
                    {
                        IdentifierId = existingRoleTypeId,
                        Name = "oldname",
                        Code = "oldcode",
                    },
                    Site = new PartyRoleSiteDocument()
                    {
                        IdentifierId = GoldSiteId,Name = "oldsitename", State = "oldstate"
                        , Type = new PremisesTypeSummaryDocument { IdentifierId = "any-ptsd-id", Code = "ptsdcode", Description = "ptsd-desc" }
                    }
                },
            new ()
            {
                IdentifierId = "anyguid",
                Role = new PartyRoleRoleDocument()
                {
                    IdentifierId = newRoleTypeId,
                    Name = "newtypename",
                    Code = "newtypecode",
                },
                Site = new PartyRoleSiteDocument()
                {
                    IdentifierId = GoldSiteId,
                    Name = null,
                    State = null,
                    Type = null
                }
            }
        };

        _goldRepoMock
            .Setup(r => r.FindPartyByCustomerNumber(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParty);

        var result = await WhenIMapNewPartyToGold(inputParty, existingPartyIds: new List<string> { customerId });

        WipeIdsAndLastUpdatedDates(result);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    private static PartyDocument CreateNewEmptyPartyDocument()
    {
        return new PartyDocument()
        {
            Id = "",
            CustomerNumber = "",
            State = "active",
            PartyType = "",
            Communication = new List<Core.Documents.CommunicationDocument>
            {
                new Core.Documents.CommunicationDocument {
                    IdentifierId = "",
                    PrimaryContactFlag = false
                }
            },
            CorrespondanceAddress = new Core.Documents.AddressDocument
            {
                IdentifierId = "",
                AddressLine1 = "",
                Postcode = "",
            }
        };
    }

    private async Task<PartyDocument> WhenIMapNewPartyToGold(SamPartyDocument inputParty, string id = "goldSiteId", List<string>? existingPartyIds = null)
    {
        existingPartyIds = existingPartyIds ?? new List<string>();
        return (await SamPartyMapper.ToGold(
            existingPartyIds,
            GoldSiteId,
            new List<SamPartyDocument> { inputParty },
            new List<SiteGroupMarkRelationshipDocument>(),
            _goldRepoMock.Object,
            _getCountryById,
            _getSpeciesById,
            CancellationToken.None)).Single();
    }

    // TODO test representative.PremiseTypeIdentifier != site.Type?.Id

    /// <summary>
    /// For comparing objects in test assertion, a destructive action that wipes unpredictable fields so the rest can be compared naturally
    /// </summary>
    private void WipeIdsAndLastUpdatedDates(PartyDocument record)
    {
        record.Id = "";
        record.LastUpdatedDate = DateTime.MinValue;
        foreach (var c in record.Communication)
        {
            c.LastUpdatedDate = DateTime.MinValue;
            c.IdentifierId = "";
        }

        if (record.CorrespondanceAddress != null)
        {
            record.CorrespondanceAddress.IdentifierId = "";
            record.CorrespondanceAddress.LastUpdatedDate = DateTime.MinValue;
            if (record.CorrespondanceAddress.Country != null)
            {
                record.CorrespondanceAddress.Country.LastModifiedDate = DateTime.MinValue;
            }
        }

        foreach (var pr in record.PartyRoles ?? [])
        {
            pr.IdentifierId = "";
            pr.Site!.LastUpdatedDate = DateTime.MinValue;
            pr.Role.LastUpdatedDate = DateTime.MinValue;
            pr.LastUpdatedDate = DateTime.MinValue;
        }
    }
}