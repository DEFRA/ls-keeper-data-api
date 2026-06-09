using KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using MongoDB.Driver;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Holdings.Steps;

public class SamHoldingImportGoldMappingStepTests
{
    [Fact]
    public async Task FindAndUpdateMainSiteIfExists_AddsAssociatedCommonLand_WhenMainSiteHasNullAssociatedCommonLands()
    {
        var goldSiteRepoMock = new Mock<IGenericRepository<SiteDocument>>();
        var partiesRepoMock = new Mock<IPartiesRepository>();

        var representative = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            LocalAuthorityName = "LA",
            AssociatedMainHoldings = new List<AssociatedHoldingRelationship>
            {
                new AssociatedHoldingRelationship { HoldingIdentifier = "MAIN-1", ContiguousFlag = true }
            }
        };

        var mainSite = new SiteDocument
        {
            Id = "site-1",
            Identifiers = new List<Core.Documents.SiteIdentifierDocument>
            {
                new Core.Documents.SiteIdentifierDocument { Identifier = "MAIN-1", IdentifierId = "id-1", Type = new Core.Documents.SiteIdentifierSummaryDocument { IdentifierId = "type-1", Code = "CPH", Name = "CPH Number" }, LastUpdatedDate = DateTime.UtcNow }
            },
            AssociatedCommonLands = new List<AssociatedHoldingDocument>() // use empty list (property non-nullable)
        };

        goldSiteRepoMock.Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainSite);

        var context = new SamHoldingImportContext
        {
            Cph = "CPH-1",
            SilverHoldings = new List<SamHoldingDocument> { representative },
            SilverHerds = new List<SamHerdDocument>(),
            SilverParties = new List<SamPartyDocument>(),
            GoldSite = new SiteDocument { Id = "rep-site" }
        };

        var mappingStep = new SamHoldingImportGoldMappingStep(
            Mock.Of<KeeperData.Core.Services.ICountryIdentifierLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISpeciesTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteActivityTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteIdentifierTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeDerivedCodeLookupService>(),
            goldSiteRepoMock.Object,
            partiesRepoMock.Object,
            new Mock<ILogger<SamHoldingImportGoldMappingStep>>().Object);

        await mappingStep.ExecuteAsync(context, CancellationToken.None);

        Assert.NotNull(context.AssociatedMainSites);
        Assert.Single(context.AssociatedMainSites);
        var updated = context.AssociatedMainSites[0];
        Assert.Equal("site-1", updated.Id);
        Assert.NotNull(updated.AssociatedCommonLands);
        Assert.Contains(updated.AssociatedCommonLands, a => a.HoldingIdentifier == "CPH-1");
    }

    [Fact]
    public async Task FindAndUpdateMainSiteIfExists_DoesNotAddDuplicateAssociatedCommonLand_WhenAlreadyPresent()
    {
        var goldSiteRepoMock = new Mock<IGenericRepository<SiteDocument>>();
        var partiesRepoMock = new Mock<IPartiesRepository>();

        var representative = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            LocalAuthorityName = "LA",
            AssociatedMainHoldings = new List<AssociatedHoldingRelationship>
            {
                new AssociatedHoldingRelationship { HoldingIdentifier = "MAIN-1", ContiguousFlag = true }
            }
        };

        var mainSite = new SiteDocument
        {
            Id = "site-1",
            Identifiers = new List<Core.Documents.SiteIdentifierDocument>
            {
                new Core.Documents.SiteIdentifierDocument { Identifier = "MAIN-1", IdentifierId = "id-1", Type = new Core.Documents.SiteIdentifierSummaryDocument { IdentifierId = "type-1", Code = "CPH", Name = "CPH Number" }, LastUpdatedDate = DateTime.UtcNow }
            },
            AssociatedCommonLands = new List<AssociatedHoldingDocument>
            {
                new AssociatedHoldingDocument { HoldingIdentifier = "CPH-1", ContiguousFlag = true }
            }
        };

        goldSiteRepoMock.Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainSite);

        var context = new SamHoldingImportContext
        {
            Cph = "CPH-1",
            SilverHoldings = new List<SamHoldingDocument> { representative },
            SilverHerds = new List<SamHerdDocument>(),
            SilverParties = new List<SamPartyDocument>(),
            GoldSite = new SiteDocument { Id = "rep-site" }
        };

        var mappingStep = new SamHoldingImportGoldMappingStep(
            Mock.Of<KeeperData.Core.Services.ICountryIdentifierLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISpeciesTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteActivityTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteIdentifierTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeDerivedCodeLookupService>(),
            goldSiteRepoMock.Object,
            partiesRepoMock.Object,
            new Mock<ILogger<SamHoldingImportGoldMappingStep>>().Object);

        await mappingStep.ExecuteAsync(context, CancellationToken.None);

        Assert.NotNull(context.AssociatedMainSites);
        Assert.Single(context.AssociatedMainSites);
        var updated = context.AssociatedMainSites[0];
        Assert.Equal("site-1", updated.Id);
        // Should not create a duplicate entry for the same holding identifier
        Assert.Equal(1, updated.AssociatedCommonLands.Count(a => a.HoldingIdentifier == "CPH-1"));
    }

    [Fact]
    public async Task FindAndUpdateMainSiteIfExists_ReplacesExistingContextEntry_WhenSiteAlreadyInContext()
    {
        var goldSiteRepoMock = new Mock<IGenericRepository<SiteDocument>>();
        var partiesRepoMock = new Mock<IPartiesRepository>();

        var representative = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            LocalAuthorityName = "LA",
            AssociatedMainHoldings = new List<AssociatedHoldingRelationship>
            {
                new AssociatedHoldingRelationship { HoldingIdentifier = "MAIN-1", ContiguousFlag = true }
            }
        };

        var mainSite = new SiteDocument
        {
            Id = "site-1",
            Identifiers = new List<Core.Documents.SiteIdentifierDocument>
            {
                new Core.Documents.SiteIdentifierDocument { Identifier = "MAIN-1", IdentifierId = "id-1", Type = new Core.Documents.SiteIdentifierSummaryDocument { IdentifierId = "type-1", Code = "CPH", Name = "CPH Number" }, LastUpdatedDate = DateTime.UtcNow }
            },
            AssociatedCommonLands = new List<AssociatedHoldingDocument>()
        };

        goldSiteRepoMock.Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainSite);

        var preExisting = new SiteDocument { Id = "site-1", AssociatedCommonLands = new List<AssociatedHoldingDocument>() };

        var context = new SamHoldingImportContext
        {
            Cph = "CPH-1",
            SilverHoldings = new List<SamHoldingDocument> { representative },
            SilverHerds = new List<SamHerdDocument>(),
            SilverParties = new List<SamPartyDocument>(),
            GoldSite = new SiteDocument { Id = "rep-site" },
            AssociatedMainSites = new List<SiteDocument> { preExisting }
        };

        var mappingStep = new SamHoldingImportGoldMappingStep(
            Mock.Of<KeeperData.Core.Services.ICountryIdentifierLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISpeciesTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteActivityTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteIdentifierTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeDerivedCodeLookupService>(),
            goldSiteRepoMock.Object,
            partiesRepoMock.Object,
            new Mock<ILogger<SamHoldingImportGoldMappingStep>>().Object);

        await mappingStep.ExecuteAsync(context, CancellationToken.None);

        Assert.NotNull(context.AssociatedMainSites);
        Assert.Single(context.AssociatedMainSites);
        var replaced = context.AssociatedMainSites[0];
        Assert.Equal("site-1", replaced.Id);
        Assert.NotNull(replaced.AssociatedCommonLands);
        Assert.Contains(replaced.AssociatedCommonLands, a => a.HoldingIdentifier == "CPH-1");
    }

    [Fact]
    public async Task EnrichWithCommonLandData_MergesLocalAuthorityName_WhenMultipleSilverHoldings()
    {
        var goldSiteRepoMock = new Mock<IGenericRepository<SiteDocument>>();
        var partiesRepoMock = new Mock<IPartiesRepository>();

        var samHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Sheep Farm",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow,
            LocalAuthorityName = null
        };

        var commonLandHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow.AddDays(-1),
            LocalAuthorityName = "Devon County Council"
        };

        goldSiteRepoMock.Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteDocument?)null);

        var context = new SamHoldingImportContext
        {
            Cph = "CPH-1",
            SilverHoldings = new List<SamHoldingDocument> { samHolding, commonLandHolding },
            SilverHerds = new List<SamHerdDocument>(),
            SilverParties = new List<SamPartyDocument>(),
            GoldSite = new SiteDocument { Id = "gold-site-1" }
        };

        var mappingStep = new SamHoldingImportGoldMappingStep(
            Mock.Of<KeeperData.Core.Services.ICountryIdentifierLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISpeciesTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteActivityTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteIdentifierTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeDerivedCodeLookupService>(),
            goldSiteRepoMock.Object,
            partiesRepoMock.Object,
            new Mock<ILogger<SamHoldingImportGoldMappingStep>>().Object);

        await mappingStep.ExecuteAsync(context, CancellationToken.None);

        Assert.Equal("Devon County Council", context.GoldSite.LocalAuthorityName);
    }

    [Fact]
    public async Task EnrichWithCommonLandData_MergesAssociatedMainHoldings_WhenMultipleSilverHoldings()
    {
        var goldSiteRepoMock = new Mock<IGenericRepository<SiteDocument>>();
        var partiesRepoMock = new Mock<IPartiesRepository>();

        var samHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Sheep Farm",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow,
            AssociatedMainHoldings = new List<AssociatedHoldingRelationship>()
        };

        var commonLandHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow.AddDays(-1),
            AssociatedMainHoldings = new List<AssociatedHoldingRelationship>
            {
                new AssociatedHoldingRelationship { HoldingIdentifier = "MAIN-1", ContiguousFlag = true, StartDate = "2024-01-01" },
                new AssociatedHoldingRelationship { HoldingIdentifier = "MAIN-2", ContiguousFlag = false, StartDate = "2024-02-01" }
            }
        };

        goldSiteRepoMock.Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteDocument?)null);

        var context = new SamHoldingImportContext
        {
            Cph = "CPH-1",
            SilverHoldings = new List<SamHoldingDocument> { samHolding, commonLandHolding },
            SilverHerds = new List<SamHerdDocument>(),
            SilverParties = new List<SamPartyDocument>(),
            GoldSite = new SiteDocument { Id = "gold-site-1" }
        };

        var mappingStep = new SamHoldingImportGoldMappingStep(
            Mock.Of<KeeperData.Core.Services.ICountryIdentifierLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISpeciesTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteActivityTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteIdentifierTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeDerivedCodeLookupService>(),
            goldSiteRepoMock.Object,
            partiesRepoMock.Object,
            new Mock<ILogger<SamHoldingImportGoldMappingStep>>().Object);

        await mappingStep.ExecuteAsync(context, CancellationToken.None);

        Assert.NotNull(context.GoldSite.AssociatedMainHoldings);
        Assert.Equal(2, context.GoldSite.AssociatedMainHoldings.Count);
        Assert.Contains(context.GoldSite.AssociatedMainHoldings, h => h.HoldingIdentifier == "MAIN-1");
        Assert.Contains(context.GoldSite.AssociatedMainHoldings, h => h.HoldingIdentifier == "MAIN-2");
    }

    [Fact]
    public async Task SelectRepresentativeHolding_ReturnsActiveCommonLand_WhenAllHoldingsAreCommonLandAndOneIsActive()
    {
        // Arrange: all holdings are Common Land (Priorities 1 & 2 fail), one is active
        var goldSiteRepoMock = new Mock<IGenericRepository<SiteDocument>>();
        var partiesRepoMock = new Mock<IPartiesRepository>();

        var activeCommonLand = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "ACTIVE-CPH",
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "active",
            LastUpdatedDate = DateTime.UtcNow
        };

        var inactiveCommonLand = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "INACTIVE-CPH",
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "inactive",
            LastUpdatedDate = DateTime.UtcNow.AddDays(-1)
        };

        SiteDocument? capturedFilter = null;
        goldSiteRepoMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteDocument?)null);

        var context = new SamHoldingImportContext
        {
            Cph = "ACTIVE-CPH",
            SilverHoldings = new List<SamHoldingDocument> { inactiveCommonLand, activeCommonLand },
            SilverHerds = new List<SamHerdDocument>(),
            SilverParties = new List<SamPartyDocument>(),
            GoldSite = new SiteDocument { Id = "pre-set" }
        };

        var mappingStep = new SamHoldingImportGoldMappingStep(
            Mock.Of<KeeperData.Core.Services.ICountryIdentifierLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISpeciesTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteActivityTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteIdentifierTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeDerivedCodeLookupService>(),
            goldSiteRepoMock.Object,
            partiesRepoMock.Object,
            new Mock<ILogger<SamHoldingImportGoldMappingStep>>().Object);

        // Act
        await mappingStep.ExecuteAsync(context, CancellationToken.None);

        // Assert: the active Common Land holding was selected as representative,
        // so the existing-site filter used its CPH and GoldSiteId was assigned.
        // FindOneByFilterAsync is called once, meaning representative selection completed.
        goldSiteRepoMock.Verify(
            r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // The GoldSiteId is always assigned when SilverHoldings is non-empty
        Assert.NotNull(context.GoldSiteId);

        // The representative's CPH drives the context Cph; confirm it matches the active holding
        Assert.Equal("ACTIVE-CPH", context.Cph);
    }

    [Fact]
    public async Task EnrichWithCommonLandData_DeduplicatesAssociatedMainHoldings_WhenSameIdentifierInMultipleHoldings()
    {
        var goldSiteRepoMock = new Mock<IGenericRepository<SiteDocument>>();
        var partiesRepoMock = new Mock<IPartiesRepository>();

        var samHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Sheep Farm",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow,
            AssociatedMainHoldings = new List<AssociatedHoldingRelationship>
            {
                new AssociatedHoldingRelationship { HoldingIdentifier = "MAIN-1", ContiguousFlag = true, StartDate = "2024-01-01" }
            }
        };

        var commonLandHolding = new SamHoldingDocument
        {
            CountyParishHoldingNumber = "CPH-1",
            SourceFacilitySubBusinessActivityCode = "Common Land",
            HoldingStatus = "Active",
            LastUpdatedDate = DateTime.UtcNow.AddDays(-1),
            AssociatedMainHoldings = new List<AssociatedHoldingRelationship>
            {
                new AssociatedHoldingRelationship { HoldingIdentifier = "MAIN-1", ContiguousFlag = false, StartDate = "2024-03-01" }
            }
        };

        goldSiteRepoMock.Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteDocument?)null);

        var context = new SamHoldingImportContext
        {
            Cph = "CPH-1",
            SilverHoldings = new List<SamHoldingDocument> { samHolding, commonLandHolding },
            SilverHerds = new List<SamHerdDocument>(),
            SilverParties = new List<SamPartyDocument>(),
            GoldSite = new SiteDocument { Id = "gold-site-1" }
        };

        var mappingStep = new SamHoldingImportGoldMappingStep(
            Mock.Of<KeeperData.Core.Services.ICountryIdentifierLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISpeciesTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteActivityTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteIdentifierTypeLookupService>(),
            Mock.Of<KeeperData.Core.Services.ISiteTypeDerivedCodeLookupService>(),
            goldSiteRepoMock.Object,
            partiesRepoMock.Object,
            new Mock<ILogger<SamHoldingImportGoldMappingStep>>().Object);

        await mappingStep.ExecuteAsync(context, CancellationToken.None);

        Assert.NotNull(context.GoldSite.AssociatedMainHoldings);
        Assert.Single(context.GoldSite.AssociatedMainHoldings);
        var mainHolding = context.GoldSite.AssociatedMainHoldings[0];
        Assert.Equal("MAIN-1", mainHolding.HoldingIdentifier);
        // Should prefer the most recent StartDate (2024-03-01)
        Assert.Equal("2024-03-01", mainHolding.StartDate);
    }
}