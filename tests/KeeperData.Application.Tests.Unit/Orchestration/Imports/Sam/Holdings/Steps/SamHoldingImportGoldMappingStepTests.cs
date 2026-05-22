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
}
