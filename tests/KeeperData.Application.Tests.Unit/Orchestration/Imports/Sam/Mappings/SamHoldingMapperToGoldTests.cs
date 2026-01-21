using System.Diagnostics;
using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using AddressDocument = KeeperData.Core.Documents.AddressDocument;
using LocationDocument = KeeperData.Core.Documents.LocationDocument;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamHoldingMapperToGoldTests
{
    private Func<string?, CancellationToken, Task<CountryDocument?>> _getCountryById;
    private Func<string?, CancellationToken, Task<PremisesTypeDocument?>> _getPremiseTypeById;
    private Func<string?, CancellationToken, Task<SiteIdentifierTypeDocument?>> _getSiteIdentifierTypeByCode;
    private Func<string?, CancellationToken, Task<(string? speciesTypeId, string? speciesTypeName)>> _getSpeciesByCode;
    private Func<string?, CancellationToken, Task<PremisesActivityTypeDocument?>> _getPremiseActivityTypeByCode;

    const string GoldSiteId = "gold-site-id";

    private List<CountryDocument> _countryData =
    [
        new CountryDocument
        {
            IdentifierId = "en123", Code = "GB-ENG", Name = "England", LongName = "England - United Kingdom"
        },
        new CountryDocument { IdentifierId = "fr123", Code = "FR", Name = "France", LongName = "French Republic" },
        new CountryDocument { IdentifierId = "nz123", Code = "NZ", Name = "New Zealand", LongName = "New Zealand" }
    ];

    private List<SpeciesDocument> _speciesData =
    [
        new SpeciesDocument { IdentifierId = "spec-1-id", Code = "spec1code", Name = "spec1name" },
        new SpeciesDocument { IdentifierId = "spec-2-id", Code = "spec2code", Name = "spec2name" },
        new SpeciesDocument { IdentifierId = "spec-3-id", Code = "spec3code", Name = "spec3name" }
    ];

    private List<PremisesTypeDocument> _premiseTypeData =
    [
        new PremisesTypeDocument { IdentifierId = "prem-1-id", Code = "prem1code", Name = "prem1name" },
        new PremisesTypeDocument { IdentifierId = "prem-2-id", Code = "prem2code", Name = "prem2name" },
        new PremisesTypeDocument { IdentifierId = "prem-3-id", Code = "prem3code", Name = "prem3name" }
    ];

    private List<PremisesActivityTypeDocument> _activityData =
    [
        new PremisesActivityTypeDocument { IdentifierId = "act-1-id", Code = "act1code", Name = "act1name" },
        new PremisesActivityTypeDocument { IdentifierId = "act-2-id", Code = "act2code", Name = "act2name" },
        new PremisesActivityTypeDocument { IdentifierId = "act-3-id", Code = "act3code", Name = "act3name" }
    ];

    public SamHoldingMapperToGoldTests()
    {
        _getCountryById = (string? key, CancellationToken token) =>
            Task.FromResult(_countryData.SingleOrDefault(x => x.IdentifierId == key));
        _getSpeciesByCode = (string? key, CancellationToken token) =>
        {
            var match = _speciesData.SingleOrDefault(x => x.Code == key);
            return Task.FromResult<(string? speciesTypeId, string? speciesTypeName)>((match?.IdentifierId, match?.Name));
        };
        ;
        _getPremiseTypeById = (key, token) => Task.FromResult<PremisesTypeDocument?>(_premiseTypeData.SingleOrDefault(x => x.IdentifierId == key));
        _getSiteIdentifierTypeByCode = (s, token) => Task.FromResult<SiteIdentifierTypeDocument?>(null);
        _getPremiseActivityTypeByCode = (key, token) => Task.FromResult<PremisesActivityTypeDocument?>(_activityData.SingleOrDefault(x => x.Code == key));
    }

    public static IEnumerable<object[]> TestDataForNewGoldMappings
    {
        get
        {
            yield return
                ["When mapping empty SamHoldingDocument", (SamHoldingDocument s) => { }, (SiteDocument d) => { }];
            yield return
            ["When mapping SamHoldingDocument with unknown premise",
                (SamHoldingDocument s) => { s.PremiseTypeIdentifier = "prem-invalid-id"; },
                (SiteDocument d) => {}];
            yield return
            ["When mapping SamHoldingDocument with premise",
                (SamHoldingDocument s) => { s.PremiseTypeIdentifier = "prem-1-id"; },
                (SiteDocument d) =>
                {
                    d.Type = new PremisesTypeSummaryDocument()
                    {
                        Code = "prem1code", Description = "prem1name", IdentifierId = "prem-1-id"
                    };
                }];
            yield return
            ["When mapping SamHoldingDocument with species",
                (SamHoldingDocument s) => { s.SpeciesTypeCode = "spec1code"; },
                (SiteDocument d) =>
                {
                    d.Species = [new SpeciesSummaryDocument(){ IdentifierId = "spec-1-id", Code = "spec1code", Name = "spec1name" }];
                }];
            yield return
            ["When mapping SamHoldingDocument with activity",
                (SamHoldingDocument s) => { s.PremiseActivityTypeCode = "act1code"; },
                (SiteDocument d) =>
                {
                    d.Activities = [new SiteActivityDocument()
                    {
                        IdentifierId = "act-1-id",
                        Type = new PremisesActivityTypeSummaryDocument()
                        {
                            IdentifierId = "act-1-id",
                            Code = "act1code",
                            Name = "act1name"
                        },
                        StartDate = DateTime.MinValue
                    }];
                }];
            yield return
            ["When mapping SamHoldingDocument with location info",
                (SamHoldingDocument s) => { s.Location = new Core.Documents.Silver.LocationDocument()
                {
                    IdentifierId = "loc-id",
                    Address = new Core.Documents.Silver.AddressDocument()
                    {
                        IdentifierId = "addr-id",
                        AddressLine = "line1",
                        AddressStreet = "street",
                        AddressTown = "town",
                        AddressLocality = "locale",
                        AddressPostCode = "postcode",
                        CountryIdentifier = "fr123"
                    },
                    Easting = 400030,
                    Northing = 138305,
                    OsMapReference = "SU087290"
                }; },
                (SiteDocument d) =>
                {
                    d.Location = new LocationDocument() {
                        Address = new AddressDocument
                        {
                            AddressLine1 = "line1",
                            AddressLine2 = "street",
                            PostTown = "town",
                            County = "locale",
                            Country = new CountrySummaryDocument() { IdentifierId = "fr123", Code = "FR", Name= "France", LongName = "French Republic" },
                            Postcode = "postcode",
                            IdentifierId = "any-guid"
                        },
                        IdentifierId = "any-guid",
                        Communication = new List<Core.Documents.CommunicationDocument>() { new KeeperData.Core.Documents.CommunicationDocument() { IdentifierId = "", PrimaryContactFlag = false }},
                        Easting = 400030,
                        Northing = 138305,
                        OsMapReference = "SU087290"
                    };
                }];

            // with cphn site identifier type

            //TODO is it possible for a premise to change its type during its lifetime? (our code says no)
        }
    }

    [Fact]
    public async Task WhenMappingEmptyListOfSamHoldingDocument()
    {
        var result = await SamHoldingMapper.ToGold(
            GoldSiteId,
            null,
            new List<SamHoldingDocument>() { },
            new List<SiteGroupMarkRelationshipDocument>(),
            new List<PartyDocument>(),
            _getCountryById,
            _getPremiseTypeById,
            _getSiteIdentifierTypeByCode,
            _getSpeciesByCode,
            _getPremiseActivityTypeByCode,
            CancellationToken.None
        );

        result.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(TestDataForNewGoldMappings))]
    public async Task ToGoldShouldCreateSiteWithCorrectMapping(string testName, Action<SamHoldingDocument> modifyInput, Action<SiteDocument> modifyExpected)
    {
        var inputParty = new SamHoldingDocument();
        var expected = GetBlankSiteDocument();

        Debug.WriteLine($"in testcase {testName}");
        modifyInput(inputParty);
        modifyExpected(expected);

        var result = await WhenIMapSilverSiteToGold(inputParty, null);

        WipeIdsAndLastUpdatedDates(result!);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(TestDataForNewGoldMappings))]
    public async Task ToGoldShouldUpdateSiteWithCorrectMapping(string testName, Action<SamHoldingDocument> modifyInput, Action<SiteDocument> modifyExpected)
    {
        var inputHolding = new SamHoldingDocument();
        var existingSite = new SiteDocument() { Id = GoldSiteId };
        var expected = GetBlankSiteDocument();

        Debug.WriteLine($"in testcase {testName}");
        modifyInput(inputHolding);
        modifyExpected(expected);

        var result = await WhenIMapSilverSiteToGold(inputHolding, existingSite);

        WipeIdsAndLastUpdatedDates(result!);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("should map 1 species correctly", new string[] { "spec1code" }, new string[] { "spec1code" })]
    [InlineData("should map 2 duplicated species correctly", new string[] { "spec1code", "spec1code" }, new string[] { "spec1code" })]
    [InlineData("should map 2 unique species correctly", new string[] { "spec1code", "spec2code" }, new string[] { "spec1code", "spec2code" })]
    [InlineData("should map a mix of duplicated, null, and unique species correctly", new string?[] { "spec1code", "spec2code", null, "spec1code" }, new string[] { "spec1code", "spec2code" })]
    public async Task WhenMappingMultipleHoldingsWithDistinctSpecies(string testname, string?[] inputCodes, string[] expectedCodes)
    {
        Debug.WriteLine($"in testcase {testname}");

        var samHoldingDocuments = inputCodes.Select(i => new SamHoldingDocument() { SpeciesTypeCode = i }).ToList();
        var expected = GetBlankSiteDocument();
        expected.Species = new List<SpeciesSummaryDocument>();
        foreach (var code in expectedCodes)
        {
            var doc = _speciesData.Single(sd => sd.Code == code);
            expected.Species.Add(new SpeciesSummaryDocument() { IdentifierId = doc.IdentifierId, Code = doc.Code, Name = doc.Name });
        }

        var result = await WhenIMapSilverSitesToGold(samHoldingDocuments, null);

        WipeIdsAndLastUpdatedDates(result!);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenMappingMultipleHoldingsWithConsecutiveActivities_ActivitiesShouldAllBeListedForEntireDuration()
    {
        // TODO this looks like a defect.
        // are all activities present for all time? - cannot list a location as having a particular activity for a limited time; all activities are for all time
        // only the most recent (representative) "active" holding activity dates are used

        // this test made an assumption about the format of incoming data
        // there is an alternative where there's even more duplication, with every activity listed for every subdivided period, but that also looks wrong, see next test

        // INPUT
        // timeline:  2001  -  2002  -  2003  -  2004
        // act1 -     <---------------->
        // act2 -                       <--------------------- : active
        // act3 -              <---------------->

        // OUTPUT
        // timeline:  2001  -  2002  -  2003  -  2004
        // act1 -                       <---------------------
        // act2 -                       <---------------------
        // act3 -                       <---------------------
        var samHoldingDocuments = new List<SamHoldingDocument>()
        {
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act1code",
                HoldingStartDate = new DateTime(2001, 01, 01),
                HoldingEndDate = new DateTime(2003, 01, 01), // stopped doing this in 2003
                HoldingStatus = "inactive"
            },
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act2code",
                HoldingStartDate = new DateTime(2003, 01, 01), // consecutive to act1, no terminating date
                HoldingEndDate = null,
                HoldingStatus = "active"
            },
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act3code",
                HoldingStartDate = new DateTime(2002, 01, 01), // overlapping with act2 but terminated in 2004
                HoldingEndDate = new DateTime(2004, 01, 01),
                HoldingStatus = "inactive"
            }
        };

        var expected = GetBlankSiteDocument();
        expected.Activities = new List<SiteActivityDocument>
        {
            new SiteActivityDocument { IdentifierId = "act-1-id", Type = new PremisesActivityTypeSummaryDocument { IdentifierId = "act-1-id", Code = "act1code", Name = "act1name" }, StartDate = new DateTime(2003,01,01), EndDate = null },
            new SiteActivityDocument { IdentifierId = "act-2-id", Type = new PremisesActivityTypeSummaryDocument { IdentifierId = "act-2-id", Code = "act2code", Name = "act2name" }, StartDate = new DateTime(2003,01,01), EndDate = null },
            new SiteActivityDocument { IdentifierId = "act-3-id", Type = new PremisesActivityTypeSummaryDocument { IdentifierId = "act-3-id", Code = "act3code", Name = "act3name" }, StartDate = new DateTime(2003,01,01), EndDate = null }
        };
        expected.State = "active";
        expected.StartDate = new DateTime(2003, 01, 01);

        var result = await WhenIMapSilverSitesToGold(samHoldingDocuments, null);

        WipeIdsAndLastUpdatedDates(result!);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task WhenMappingMultipleHoldingsWithConsecutiveActivities_ActivitiesShouldAllBeListedForEntireDuration2()
    {
        // TODO this looks like a defect.
        // are all activities present for all time? - cannot list a location as having a particular activity for a limited time; all activities are for all time
        // only the most recent (representative) "active" holding activity dates are used

        // this test made an assumption about the format of incoming data
        // there is an alternative where there's even more duplication, with every activity listed for every subdivided period, but that also looks wrong, see next test

        // INPUT
        // timeline:  2001  -  2002  -  2003  -  2004
        // act1 -     <-------><------->
        // act2 -                       <-------><----------- : active
        // act3 -              <-------><------->

        // OUTPUT
        // timeline:  2001  -  2002  -  2003  -  2004
        // act1 -                                <-------------
        // act2 -                                <-------------
        // act3 -                                <-------------
        var samHoldingDocuments = new List<SamHoldingDocument>()
        {
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act1code",
                HoldingStartDate = new DateTime(2001, 01, 01),
                HoldingEndDate = new DateTime(2002, 01, 01),
                HoldingStatus = "inactive"
            },
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act1code",
                HoldingStartDate = new DateTime(2002, 01, 01),
                HoldingEndDate = new DateTime(2003, 01, 01),
                HoldingStatus = "inactive"
            },
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act2code",
                HoldingStartDate = new DateTime(2003, 01, 01),
                HoldingEndDate = new DateTime(2004, 01, 01),
                HoldingStatus = "inactive"
            },
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act2code",
                HoldingStartDate = new DateTime(2004, 01, 01),
                HoldingEndDate = null,
                HoldingStatus = "active"
            },
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act3code",
                HoldingStartDate = new DateTime(2002, 01, 01),
                HoldingEndDate = new DateTime(2003, 01, 01),
                HoldingStatus = "inactive"
            },
            new SamHoldingDocument()
            {
                PremiseActivityTypeCode = "act3code",
                HoldingStartDate = new DateTime(2003, 01, 01),
                HoldingEndDate = new DateTime(2004, 01, 01),
                HoldingStatus = "inactive"
            }
        };

        var expected = GetBlankSiteDocument();
        expected.Activities = new List<SiteActivityDocument>
        {
            new SiteActivityDocument { IdentifierId = "act-1-id", Type = new PremisesActivityTypeSummaryDocument { IdentifierId = "act-1-id", Code = "act1code", Name = "act1name" }, StartDate = new DateTime(2004,01,01), EndDate = null },
            new SiteActivityDocument { IdentifierId = "act-2-id", Type = new PremisesActivityTypeSummaryDocument { IdentifierId = "act-2-id", Code = "act2code", Name = "act2name" }, StartDate = new DateTime(2004,01,01), EndDate = null },
            new SiteActivityDocument { IdentifierId = "act-3-id", Type = new PremisesActivityTypeSummaryDocument { IdentifierId = "act-3-id", Code = "act3code", Name = "act3name" }, StartDate = new DateTime(2004,01,01), EndDate = null }
        };
        expected.State = "active";
        expected.StartDate = new DateTime(2004, 01, 01);

        var result = await WhenIMapSilverSitesToGold(samHoldingDocuments, null);

        WipeIdsAndLastUpdatedDates(result!);
        WipeIdsAndLastUpdatedDates(expected);
        result.Should().BeEquivalentTo(expected);
    }

    private static SiteDocument GetBlankSiteDocument()
    {
        return new SiteDocument()
        {
            Id = GoldSiteId,
            Name = "",
            Source = "SAM",
            Location = new LocationDocument()
            {
                IdentifierId = "any-guid",
                Address = new AddressDocument()
                {
                    AddressLine1 = "",
                    IdentifierId = "any-guid",
                    Postcode = ""
                },
                Communication = [new KeeperData.Core.Documents.CommunicationDocument()
                {
                    IdentifierId = "any-guid",
                    PrimaryContactFlag = false
                }]
            }
        };
    }

    private async Task<SiteDocument?> WhenIMapSilverSiteToGold(SamHoldingDocument inputHolding, SiteDocument? existingSite)
    {
        return await WhenIMapSilverSitesToGold(new List<SamHoldingDocument>() { inputHolding }, existingSite);
    }

    private async Task<SiteDocument?> WhenIMapSilverSitesToGold(List<SamHoldingDocument> inputHoldings, SiteDocument? existingSite)
    {
        return await SamHoldingMapper.ToGold(
            GoldSiteId,
            existingSite,
            inputHoldings,
            new List<SiteGroupMarkRelationshipDocument>(),
            new List<PartyDocument>(),
            _getCountryById,
            _getPremiseTypeById,
            _getSiteIdentifierTypeByCode,
            _getSpeciesByCode,
            _getPremiseActivityTypeByCode,
            CancellationToken.None
        );
    }

    private void WipeIdsAndLastUpdatedDates(SiteDocument record)
    {
        if (record.Location != null)
        {
            record.Location.IdentifierId = "";
            record.Location.LastUpdatedDate = DateTime.MinValue;

            if (record.Location.Address != null)
            {
                record.Location.Address.IdentifierId = "";
                record.Location.Address.LastUpdatedDate = DateTime.MinValue;
            }

            foreach (var comms in record.Location.Communication)
            {
                comms.IdentifierId = "";
                comms.LastUpdatedDate = DateTime.MinValue;
            }
        }

        foreach (var s in record.Species)
        {
            s.LastModifiedDate = DateTime.MinValue;
        }
    }
}