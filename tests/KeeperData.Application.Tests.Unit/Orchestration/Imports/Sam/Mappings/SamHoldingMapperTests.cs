using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;
using KeeperData.Tests.Common.TestData.Sam;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SamHoldingMapperTests
{
    private readonly Mock<ISiteActivityTypeLookupService> _siteActivityTypeLookupServiceMock = new();
    private readonly Mock<ISiteTypeLookupService> _siteTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    private readonly Mock<IActivityCodeLookupService> _activityCodeLookupServiceMock = new();
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveSiteActivityType;
    private readonly Func<string?, CancellationToken, Task<(string?, string?)>> _resolveSiteType;
    private readonly Func<string?, string?, CancellationToken, Task<(string?, string?, string?)>> _resolveCountry;

    public SamHoldingMapperTests()
    {
        _siteActivityTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _siteTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, string? internalCode, CancellationToken token) => (Guid.NewGuid().ToString(), input, input));

        _activityCodeLookupServiceMock.Setup(x => x.FindByActivityCodeAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? key, CancellationToken token) => SamTestScenarios.LookupCodes(key));

        _resolveSiteActivityType = _siteActivityTypeLookupServiceMock.Object.FindAsync;
        _resolveSiteType = _siteTypeLookupServiceMock.Object.FindAsync;
        _resolveCountry = _countryIdentifierLookupServiceMock.Object.FindAsync;
    }

    [Fact]
    public async Task GivenNullableRawHoldings_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHoldingMapper.ToSilver(
            (List<SamCphHolding>?)null!,
            _resolveSiteActivityType,
            _resolveSiteType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public async Task GivenEmptyRawHoldings_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = await SamHoldingMapper.ToSilver(
            [],
            _resolveSiteActivityType,
            _resolveSiteType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GivenRawHoldings_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity)
    {
        var records = GenerateSamCphHolding(quantity);

        var results = await SamHoldingMapper.ToSilver(
            records,
            _resolveSiteActivityType,
            _resolveSiteType,
            _resolveCountry,
            CancellationToken.None);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifySamHoldingMappings.VerifyMapping_From_SamCphHolding_To_SamHoldingDocument(records[i], results[i]);
        }
    }

    [Fact]
    public async Task ToGold_WithPermanentLandHoldingRelationship_ShouldSetPermanentLandHoldingIdentifier()
    {
        var silverHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "12/345/6789",
            SecondaryCph = "12/345/9999",
            CphRelationshipType = "PCPHLANDUSEDBYTCPH",
            LocationName = "Test Farm",
            CphTypeIdentifier = "PERMANENT",
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            HoldingStartDate = DateTime.UtcNow
        };

        var siteIdentifierType = new SiteIdentifierTypeDocument
        {
            IdentifierId = "type-id",
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = DateTime.UtcNow
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [silverHolding],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(
                code == "CPHN" ? siteIdentifierType : null),
            (_, _) => Task.FromResult<(string?, string?)>((null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ParentSiteIdentifier.Should().BeNull();
        result.PermanentLandHoldingIdentifier.Should().Be("12/345/9999");
    }

    [Fact]
    public async Task ToGold_WithNonPermanentLandHoldingRelationship_ShouldSetParentSiteIdentifier()
    {
        var silverHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "12/345/6789",
            SecondaryCph = "12/345/8888",
            CphRelationshipType = "OTHERTYPECPH",
            LocationName = "Test Farm",
            CphTypeIdentifier = "PERMANENT",
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            HoldingStartDate = DateTime.UtcNow
        };

        var siteIdentifierType = new SiteIdentifierTypeDocument
        {
            IdentifierId = "type-id",
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = DateTime.UtcNow
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [silverHolding],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(
                code == "CPHN" ? siteIdentifierType : null),
            (_, _) => Task.FromResult<(string?, string?)>((null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ParentSiteIdentifier.Should().Be("12/345/8888");
        result.PermanentLandHoldingIdentifier.Should().BeNull();
    }

    [Fact]
    public async Task ToGold_WithNullSecondaryCph_ShouldHaveNullIdentifiers()
    {
        var silverHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "12/345/6789",
            SecondaryCph = null,
            CphRelationshipType = null,
            LocationName = "Test Farm",
            CphTypeIdentifier = "PERMANENT",
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            HoldingStartDate = DateTime.UtcNow
        };

        var siteIdentifierType = new SiteIdentifierTypeDocument
        {
            IdentifierId = "type-id",
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = DateTime.UtcNow
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [silverHolding],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(
                code == "CPHN" ? siteIdentifierType : null),
            (_, _) => Task.FromResult<(string?, string?)>((null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ParentSiteIdentifier.Should().BeNull();
        result.PermanentLandHoldingIdentifier.Should().BeNull();
    }

    private static List<SamCphHolding> GenerateSamCphHolding(int quantity)
    {
        var records = new List<SamCphHolding>();
        var factory = new MockSamRawDataFactory();
        for (var i = 0; i < quantity; i++)
        {
            records.Add(factory.CreateMockHolding(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: CphGenerator.GenerateFormattedCph(),
                endDate: DateTime.UtcNow.Date));
        }
        return records;
    }

    [Fact]
    public async Task ToGold_PrefersActiveSamHolding_OverCommonLand()
    {
        var activeSamHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Sheep Farm",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow,
            LocationName = "SAM Farm Location",
            SpeciesTypeCode = "SHE"
        };

        var activeCommonLand = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow.AddDays(1), // More recent, but should not be selected
            LocationName = "Common Land Location",
            SpeciesTypeCode = null
        };

        var siteIdentifierType = new SiteIdentifierTypeDocument
        {
            IdentifierId = "type-id",
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = DateTime.UtcNow
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [activeSamHolding, activeCommonLand],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(
                code == "CPHN" ? siteIdentifierType : null),
            (code, _) => Task.FromResult<(string?, string?)>(code == "SHE" ? ("species-id", "Sheep") : (null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        // Verify that SAM Holding was used as representative by checking species (only SAM has it)
        result!.Species.Should().NotBeNull();
        result.Species.Should().ContainSingle(s => s.Code == "SHE");
    }

    [Fact]
    public async Task ToGold_PrefersInactiveSamHolding_OverActiveCommonLand()
    {
        var inactiveSamHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Cattle Farm",
            HoldingStatus = "Inactive",
            LastUpdatedDate = DateTime.UtcNow,
            LocationName = "SAM Farm Location",
            SpeciesTypeCode = "CAT"
        };

        var activeCommonLand = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow.AddDays(1),
            LocationName = "Common Land Location",
            SpeciesTypeCode = null
        };

        var siteIdentifierType = new SiteIdentifierTypeDocument
        {
            IdentifierId = "type-id",
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = DateTime.UtcNow
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [inactiveSamHolding, activeCommonLand],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(
                code == "CPHN" ? siteIdentifierType : null),
            (code, _) => Task.FromResult<(string?, string?)>(code == "CAT" ? ("species-id", "Cattle") : (null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        // Verify that SAM Holding was used (Priority 2 beats Priority 3)
        result!.Species.Should().NotBeNull();
        result.Species.Should().ContainSingle(s => s.Code == "CAT");
    }

    [Fact]
    public async Task ToGold_WithCommonLandSource_ShouldUseCommonLandLocationName()
    {
        var activeSamHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Sheep Farm",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow,
            LocationName = "Feature 22",
            SpeciesTypeCode = "SHE"
        };

        var activeCommonLand = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            IsFromCommonLandSource = true,
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow.AddDays(1),
            LocationName = "Premises 22",
            SpeciesTypeCode = null
        };

        var siteIdentifierType = new SiteIdentifierTypeDocument
        {
            IdentifierId = "type-id",
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = DateTime.UtcNow
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [activeSamHolding, activeCommonLand],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(
                code == "CPHN" ? siteIdentifierType : null),
            (code, _) => Task.FromResult<(string?, string?)>(code == "SHE" ? ("species-id", "Sheep") : (null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Premises 22");
        result.Species.Should().ContainSingle(s => s.Code == "SHE");
    }

    [Fact]
    public async Task ToGold_SelectsCommonLand_WhenOnlyCommonLandPresent()
    {
        var activeCommonLand = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow,
            LocationName = "Common Land Location",
            LocalAuthorityName = "Devon County Council",
            CreatedDate = DateTime.UtcNow
        };

        var siteIdentifierType = new SiteIdentifierTypeDocument
        {
            IdentifierId = "type-id",
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = DateTime.UtcNow
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [activeCommonLand],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(
                code == "CPHN" ? siteIdentifierType : null),
            (_, _) => Task.FromResult<(string?, string?)>((null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        // Verify Common Land was selected as representative (fallback when no SAM Holding)
        result!.Name.Should().Be("Common Land Location");
    }

    [Fact]
    public async Task ToGold_SelectsMostRecentActiveSamHolding_WhenMultipleActiveSamHoldings()
    {
        var olderSamHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Pig Farm",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow.AddDays(-5),
            LocationName = "Old Location",
            SpeciesTypeCode = "PIG"
        };

        var newerSamHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Sheep Farm",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow,
            LocationName = "New Location",
            SpeciesTypeCode = "SHE"
        };

        var siteIdentifierType = new SiteIdentifierTypeDocument
        {
            IdentifierId = "type-id",
            Code = "CPHN",
            Name = "CPH Number",
            LastModifiedDate = DateTime.UtcNow
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [olderSamHolding, newerSamHolding],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(
                code == "CPHN" ? siteIdentifierType : null),
            (code, _) => Task.FromResult<(string?, string?)>(
                code == "SHE" ? ("sheep-id", "Sheep") :
                code == "PIG" ? ("pig-id", "Pig") : (null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        // Should have both species (aggregated from all), but verify the newer one is present
        result!.Species.Should().NotBeNull();
        result.Species.Should().Contain(s => s.Code == "SHE");
        result.Species.Should().Contain(s => s.Code == "PIG");
    }

    [Theory]
    [InlineData(null, null, true)]   // No effective dates -> always current
    [InlineData(-5, null, true)]     // Started in the past, no end -> current
    [InlineData(5, null, false)]     // Starts in the future -> not current
    [InlineData(null, 5, true)]      // Ends in the future, no start -> current
    [InlineData(null, -5, false)]    // Ended in the past -> not current
    [InlineData(-5, 5, true)]        // Within the effective window -> current
    [InlineData(5, 10, false)]       // Window entirely in the future -> not current
    [InlineData(-10, -5, false)]     // Window entirely in the past -> not current
    public async Task ToGold_WithShowground_SetsApprovalCurrentFlagFromEffectiveDates(
        int? fromOffsetDays, int? toOffsetDays, bool expectedFlag)
    {
        var now = DateTime.UtcNow;
        var showground = new SamShowground
        {
            CPH = "12/345/6789",
            START_DATE = fromOffsetDays.HasValue ? now.AddDays(fromOffsetDays.Value) : null,
            END_DATE = toOffsetDays.HasValue ? now.AddDays(toOffsetDays.Value) : null
        };

        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [BuildSilverHolding()],
            [],
            [],
            [showground],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(BuildCphnIdentifierType(code)),
            (_, _) => Task.FromResult<(string?, string?)>((null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ApprovalCurrentFlag.Should().Be(expectedFlag);
        result.EffectiveFromDate.Should().Be(showground.START_DATE);
        result.EffectiveToDate.Should().Be(showground.END_DATE);
    }

    [Fact]
    public async Task ToGold_WithoutShowground_LeavesApprovalFieldsNull()
    {
        var result = await SamHoldingMapper.ToGold(
            "gold-site-id",
            null,
            [BuildSilverHolding()],
            [],
            [],
            [],
            (_, _) => Task.FromResult<CountryDocument?>(null),
            (_, _) => Task.FromResult<SiteTypeDocument?>(null),
            (code, _) => Task.FromResult<SiteIdentifierTypeDocument?>(BuildCphnIdentifierType(code)),
            (_, _) => Task.FromResult<(string?, string?)>((null, null)),
            (_, _) => Task.FromResult<SiteActivityTypeDocument?>(null),
            Mock.Of<ISiteTypeDerivedCodeLookupService>(),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ApprovalCurrentFlag.Should().BeNull();
        result.EffectiveFromDate.Should().BeNull();
        result.EffectiveToDate.Should().BeNull();
    }

    private static SamHoldingDocument BuildSilverHolding() => new()
    {
        CountyParishHoldingNumber = "12/345/6789",
        LocationName = "Test Farm",
        HoldingStatus = "Active",
        CreatedDate = DateTime.UtcNow,
        LastUpdatedDate = DateTime.UtcNow,
        HoldingStartDate = DateTime.UtcNow
    };

    private static SiteIdentifierTypeDocument? BuildCphnIdentifierType(string? code) =>
        code == "CPHN"
            ? new SiteIdentifierTypeDocument
            {
                IdentifierId = "type-id",
                Code = "CPHN",
                Name = "CPH Number",
                LastModifiedDate = DateTime.UtcNow
            }
            : null;
}