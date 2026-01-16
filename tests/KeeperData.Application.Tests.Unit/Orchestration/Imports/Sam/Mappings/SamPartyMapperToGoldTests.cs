using System.Diagnostics;
using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using MongoDB.Driver;
using Moq;
using CommunicationDocument = KeeperData.Core.Documents.Silver.CommunicationDocument;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamPartyMapperToGoldTests
{
    private Func<string?, CancellationToken, Task<CountryDocument?>> _getCountryById;
    private Func<string?, CancellationToken, Task<SpeciesDocument?>> _getSpeciesById;
    private Mock<IGenericRepository<PartyDocument>> _goldRepoMock;
    
    private List<CountryDocument> _countryData = [ 
        new CountryDocument { IdentifierId = "en123", Code = "GB-ENG", Name = "England", LongName = "England - United Kingdom"},
        new CountryDocument { IdentifierId = "fr123", Code = "FR", Name = "France", LongName = "French Republic"},
        new CountryDocument { IdentifierId = "nz123", Code = "NZ", Name = "New Zealand", LongName = "New Zealand"}];
    
    public SamPartyMapperToGoldTests()
    {
        _goldRepoMock = new Mock<IGenericRepository<PartyDocument>>();
        var species = new SpeciesDocument() { IdentifierId = "p123", Code = "P", Name = "Pig" };
        _getCountryById = (string? key, CancellationToken token) => Task.FromResult(_countryData.SingleOrDefault(x=>x.IdentifierId == key));
        _getSpeciesById = (string? key, CancellationToken token) => Task.FromResult<SpeciesDocument?>(species);
    }

    public static IEnumerable<object[]> TestDataForNewGoldMappings 
    {   get
        {
            yield return ["When mapping new null PartyDocument", (SamPartyDocument s) => {}, (PartyDocument d) => {}];
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

    [Theory]
    [InlineData("a", "newPartyId", "a")]
    public async Task ToGoldShouldNotModifyCollectionOfExistingPartyIdsWhenPartyIsNew(string? existingPartyIdsCsv, string newPartyId, string? expectedPartyIdsCsv)
    {
        var existingPartyIds = existingPartyIdsCsv.Split(',').ToList();
        var inputParty = new SamPartyDocument();
        inputParty.PartyId = newPartyId; 
        
        var result =  await WhenIMapNewPartyToGold(inputParty, existingPartyIds: existingPartyIds);
        var expectedPartyIds = expectedPartyIdsCsv.Split(',').ToList();

        result.CustomerNumber.Should().Be(newPartyId);
        existingPartyIds.Should().BeEquivalentTo(expectedPartyIds);
    }

    [Fact]
    public async Task ToGoldShouldModifyCollectionOfExistingPartyIdsWhenPartyIdExists()
    {
        var existingPartyIds = new List<string>{"a"};
        var existingPartyId = "existingPartyId";
        var expectedPartyIds = new List<string>{"a", existingPartyId};
        var inputParty = new SamPartyDocument();
        inputParty.PartyId = existingPartyId; 
        var existingParty = new PartyDocument() { Id = existingPartyId};
        _goldRepoMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParty);
        
        var result =  await WhenIMapNewPartyToGold(inputParty, existingPartyIds: existingPartyIds);

        result.CustomerNumber.Should().Be(existingPartyId);
        existingPartyIds.Should().BeEquivalentTo(expectedPartyIds);
    }

    [Theory]
    [MemberData(nameof(TestDataForNewGoldMappings))]
    public async Task ToGoldShouldUpdatePartyWithCorrectMapping(string testName, Action<SamPartyDocument> modifyInput, Action<PartyDocument> modifyExpected)
    {
        var existingId = "existing-id";
        var inputParty = new SamPartyDocument();
        inputParty.PartyId = existingId;
        var existingParty = new PartyDocument() { Id = existingId};
        _goldRepoMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParty);
        
        var expected = CreateNewEmptyPartyDocument();

        Debug.WriteLine($"in testcase {testName}");
        modifyInput(inputParty);
        modifyExpected(expected);

        expected.CustomerNumber = existingId;
        
        var result = await WhenIMapNewPartyToGold(inputParty, existingPartyIds: new List<string>{existingId});
        
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
            "gold-site-id",
            new List<SamPartyDocument> { inputParty },
            new List<SiteGroupMarkRelationshipDocument>(),
            _goldRepoMock.Object,
            _getCountryById,
            _getSpeciesById,
            CancellationToken.None)).Single();
    }

    /// <summary>
    /// For comparing objects in test assertion, a destructive action that wipes unpredictale fields so the rest can be compared naturally
    /// </summary>
    /// <param name="expected"></param>
    private void WipeIdsAndLastUpdatedDates(PartyDocument expected)
    {
        expected.Id = "";
        expected.LastUpdatedDate = DateTime.MinValue;
        foreach(var c in expected.Communication)
        { 
            c.LastUpdatedDate = DateTime.MinValue;
            c.IdentifierId = "";
        }

        if (expected.CorrespondanceAddress!= null) {
            expected.CorrespondanceAddress.IdentifierId = "";
            expected.CorrespondanceAddress.LastUpdatedDate = DateTime.MinValue;
            if (expected.CorrespondanceAddress.Country != null)
            {
                expected.CorrespondanceAddress.Country.LastModifiedDate = DateTime.MinValue;
            }
        }
    }
}