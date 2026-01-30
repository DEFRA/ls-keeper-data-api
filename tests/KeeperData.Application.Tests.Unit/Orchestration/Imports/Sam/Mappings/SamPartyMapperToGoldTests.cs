using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Moq;
using System.Diagnostics;
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
                (SamPartyDocument input) => { input.Roles = []; },
                (PartyDocument expected) => { /*no change*/ }
            ];
            yield return [ "When mapping null PartyDocument.Roles",
                (SamPartyDocument input) => { input.Roles = null; },
                (PartyDocument expected) => { /*no change*/ }
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
                                IdentifierId = GoldSiteId
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

        var result = await WhenIMapSilverPartyToGold(inputParty);

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

        var result = await WhenIMapSilverPartyToGold(inputParty, existingPartyIds: existingPartyIds);
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

        var result = await WhenIMapSilverPartyToGold(inputParty, existingPartyIds: existingPartyIds);

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

        var result = await WhenIMapSilverPartyToGold(inputParty, existingPartyIds: new List<string> { "gold-id" });

        WipeIdsAndLastUpdatedDates(result);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenOldRoleNotInListOfNewRoles_ShouldRemoveOrphanedRoles()
    {
        var updateDate = new DateTime(2020, 1, 1);
        var customerId = "customer-id";
        var newRoleId = "new-roletype-id";
        var existingRoleId = "existing-roleId";
        var inputParty = new SamPartyDocument()
        {
            PartyId = customerId,
            Roles = new List<PartyRoleDocument>
            {
                CreateRole("new", newRoleId)
            },
            LastUpdatedDate = updateDate
        };

        inputParty.PartyId = customerId;
        var existingParty = new PartyDocument()
        {
            Id = "gold-id",
            CustomerNumber = customerId,
            PartyRoles = new List<PartyRoleWithSiteDocument>()
            {
                CreatePartyRole("existing", existingRoleId)
            },
            LastUpdatedDate = DateTime.MinValue
        };

        _goldRepoMock
            .Setup(r => r.FindPartyByCustomerNumber(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParty);

        var result = await WhenIMapSilverPartyToGold(inputParty, existingPartyIds: new List<string> { customerId });

        result.LastUpdatedDate.Should().Be(updateDate);
        result.PartyRoles.Select(r => r.Role.IdentifierId).Should().BeEquivalentTo([newRoleId]);
    }

    private static PartyRoleWithSiteDocument CreatePartyRole(string roleId, string valuePrefix)
    {
        return new()
        {
            IdentifierId = roleId,
            Role = new PartyRoleRoleDocument() { IdentifierId = valuePrefix + "roleTypeId", Code = valuePrefix + "typecode", Name = valuePrefix + "typename", },
            Site = new PartyRoleSiteDocument()
            {
                IdentifierId = GoldSiteId,
                Name = null,
                State = null,
                Type = null
            }
        };
    }

    private static PartyRoleDocument CreateRole(string valuePrefix, string roleTypeId)
    {
        return new PartyRoleDocument()
        {
            IdentifierId = valuePrefix+"id",
            RoleTypeId = roleTypeId,
            RoleTypeCode = valuePrefix + "typecode",
            RoleTypeName = valuePrefix + "typename",
            SourceRoleName = valuePrefix + "sourcerolename",
        };
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

    private async Task<PartyDocument> WhenIMapSilverPartyToGold(SamPartyDocument inputParty, string id = "goldSiteId", List<string>? existingPartyIds = null)
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