using AutoFixture;
using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Holdings;

public class SamHoldingImportOrchestratorTests
{
    private readonly Mock<IGenericRepository<SamHoldingDocument>> _silverHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamHerdDocument>> _silverHerdRepositoryMock = new();

    private readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();
    private readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    private readonly Mock<IGoldSitePartyRoleRelationshipRepository> _goldSitePartyRoleRelationshipRepositoryMock = new();

    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    private readonly Mock<IPremiseActivityTypeLookupService> _premiseActivityTypeLookupServiceMock = new();
    private readonly Mock<IPremiseTypeLookupService> _premiseTypeLookupServiceMock = new();
    private readonly Mock<IProductionTypeLookupService> _productionTypeLookupServiceMock = new();
    private readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();
    private readonly Mock<ISiteIdentifierTypeLookupService> _siteIdentifierTypeLookupServiceMock = new();

    private readonly Fixture _fixture;

    public SamHoldingImportOrchestratorTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenAHoldingIdentifier_WhenExecutingSamHoldingImportOrchestrator_ShouldProcessAllStepsSuccessfully()
    {
        var (holdingIdentifier, holdings, holders, herds, parties) = new MockSamRawDataFactory().CreateMockData(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            holdingCount: 1,
            holderCount: 1,
            herdCount: 1,
            partyCount: 1);

        var (holdingsUri, herdsUri, holdersUri, partiesUri) = GetAllQueryUris(holdingIdentifier, parties.Select(x => x.PARTY_ID));

        SetupRepositoryMocks();
        SetupLookupServiceMocks();

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsScoped(_silverHoldingRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverPartyRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverHerdRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldSiteRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldPartyRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldSitePartyRoleRelationshipRepositoryMock.Object);

        // Register lookup service mocks
        factory.OverrideServiceAsScoped(_countryIdentifierLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_premiseActivityTypeLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_premiseTypeLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_productionTypeLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_productionUsageLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_roleTypeLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_speciesTypeLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_siteIdentifierTypeLookupServiceMock.Object);

        SetupDataBridgeApiRequest(factory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(holdings));
        SetupDataBridgeApiRequest(factory, herdsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(herds));
        SetupDataBridgeApiRequest(factory, holdersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(holders));
        SetupDataBridgeApiRequest(factory, partiesUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(parties));

        var result = await ExecuteTestAsync(factory, holdingIdentifier);

        VerifyDataBridgeApiEndpointCalled(factory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, herdsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, holdersUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, partiesUri, Times.Once());

        VerifyRawDataTypes(result, holdingIdentifier, holders[0].PARTY_ID, parties[0].PARTY_ID);
        VerifySilverDataTypes(result, holdingIdentifier);
        VerifyGoldDataTypes(result, holdingIdentifier);
    }

    private static async Task<SamHoldingImportContext> ExecuteTestAsync(AppWebApplicationFactory factory, string holdingIdentifier)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new SamHoldingImportContext
        {
            Cph = holdingIdentifier,
            BatchId = 1,
            CurrentDateTime = DateTime.UtcNow
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<SamHoldingImportOrchestrator>();
        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        return context;
    }

    private static void SetupDataBridgeApiRequest(AppWebApplicationFactory factory, string uri, HttpStatusCode httpStatusCode, StringContent httpResponseMessage)
    {
        factory.DataBridgeApiClientHttpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TestConstants.DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(httpStatusCode, httpResponseMessage);
    }

    private static void VerifyDataBridgeApiEndpointCalled(AppWebApplicationFactory factory, string requestUrl, Times times)
    {
        factory.DataBridgeApiClientHttpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TestConstants.DataBridgeApiBaseUrl}/{requestUrl}", times);
    }

    private static void VerifyRawDataTypes(SamHoldingImportContext context, string holdingIdentifier, string samHolderPartyId, string samPartyId)
    {
        context.RawHoldings.Should().NotBeNull().And.HaveCount(1);
        context.RawHoldings[0].CPH.Should().Be(holdingIdentifier);

        context.RawHerds.Should().NotBeNull().And.HaveCount(1);
        context.RawHerds[0].CPHH.Should().Be(holdingIdentifier);

        context.RawHolders.Should().NotBeNull().And.HaveCount(1);
        context.RawHolders[0].PARTY_ID.Should().Be(samHolderPartyId);

        context.RawParties.Should().NotBeNull().And.HaveCount(2);
        context.RawParties.Count(x => x.PARTY_ID == samPartyId).Should().Be(1);
        context.RawParties.Count(x => x.PARTY_ID == samHolderPartyId).Should().Be(1);
    }

    private static void VerifySilverDataTypes(SamHoldingImportContext context, string holdingIdentifier)
    {
        context.SilverHoldings.Should().NotBeNull().And.HaveCount(1);
        context.SilverHoldings[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);
        context.SilverHoldings[0].GroupMarks.Should().NotBeNull().And.HaveCount(1);
        context.SilverHoldings![0].GroupMarks![0].Should().NotBeNull();
        context.SilverHoldings![0].GroupMarks![0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);

        context.SilverParties.Should().NotBeNull().And.HaveCount(2);

        var distinctRoles = context.RawParties.SelectMany(x => x.RoleList)
            .Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim())
            .ToArray() ?? [];

        var roleList = context.RawParties[0].ROLES?.Split(",")
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .ToArray() ?? [];

        context.SilverPartyRoles.Should().NotBeNull().And.HaveCount(distinctRoles.Length);

        for (var i = 0; i < context.SilverPartyRoles.Count; i++)
        {
            context.SilverPartyRoles[i].HoldingIdentifier.Should().Be(holdingIdentifier);
        }

        context.SilverHerds.Should().NotBeNull().And.HaveCount(1);
        context.SilverHerds[0].CountyParishHoldingHerd.Should().Contain(holdingIdentifier);
        context.SilverHerds[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);
    }

    private static void VerifyGoldDataTypes(SamHoldingImportContext context, string holdingIdentifier)
    {
        var goldSiteId = context.GoldSite!.Id;

        context.GoldSite.Should().NotBeNull();
        context.GoldSite.Identifiers[0].Identifier.Should().Be(holdingIdentifier);

        context.GoldParties.Should().NotBeNull().And.HaveCount(2);
        for (var i = 0; i < context.GoldParties.Count; i++)
        {
            if (context.GoldParties[i].PartyRoles.Count != 0)
            {
                context.GoldParties[i].PartyRoles[0].Site!.IdentifierId.Should().Be(goldSiteId);
            }
        }

        context.GoldSitePartyRoles.Should().NotBeNull().And.HaveCount(context.SilverPartyRoles.Count);

        for (var i = 0; i < context.GoldSitePartyRoles.Count; i++)
        {
            context.GoldSitePartyRoles[i].HoldingIdentifier.Should().Be(holdingIdentifier);
        }

        for (var i = 0; i < context.GoldSiteGroupMarks.Count; i++)
        {
            context.GoldSiteGroupMarks[i].HoldingIdentifier.Should().Be(holdingIdentifier);
        }
    }

    private static (string holdingsUri, string herdsUri, string holdersUri, string partiesUri) GetAllQueryUris(string holdingIdentifier, IEnumerable<string> partyIds)
    {
        var holdingsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHoldings,
            new { },
            DataBridgeQueries.SamHoldingsByCph(holdingIdentifier));

        var herdsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHerds,
            new { },
            DataBridgeQueries.SamHerdsByCph(holdingIdentifier));

        var holdersUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHolders,
            new { },
            DataBridgeQueries.SamHoldersByCph(holdingIdentifier));

        var partiesUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamParties,
            new { },
            DataBridgeQueries.SamPartiesByPartyIds(partyIds));

        return (holdingsUri, herdsUri, holdersUri, partiesUri);
    }

    private void SetupRepositoryMocks(
        int existingSilverHoldingsCount = 0,
        int existingSilverPartiesCount = 0,
        int existingGoldSitesCount = 0,
        int existingGoldPartiesCount = 0)
    {
        var existingSilverHolding = existingSilverHoldingsCount == 0
            ? null : _fixture.Create<SamHoldingDocument>();

        var existingSilverParties = existingSilverPartiesCount == 0
            ? []
            : _fixture.CreateMany<SamPartyDocument>(existingSilverPartiesCount);

        var existingGoldSite = existingGoldSitesCount == 0
            ? null : _fixture.Create<SiteDocument>();

        var existingGoldParties = existingGoldPartiesCount == 0
            ? []
            : _fixture.CreateMany<PartyDocument>(existingGoldPartiesCount);

        // Silver Holding
        _silverHoldingRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverHoldingRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHoldingRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Party
        _silverPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SamPartyDocument?)null);

        _silverPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Herds
        _silverHerdRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHerdDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverHerdRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHerdDocument>, SamHerdDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHerdRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site
        _goldSiteRepositoryMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGoldSite);

        _goldSiteRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SiteDocument>, SiteDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Party
        _goldPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<PartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyDocument?)null);

        _goldPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<PartyDocument>, PartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site Party Role Relationships
        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.GetExistingSitePartyRoleRelationships(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private void SetupLookupServiceMocks()
    {
        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, string? internalCode, CancellationToken token) => (Guid.NewGuid().ToString(), input, input));

        _premiseActivityTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _premiseTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _productionTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _productionUsageLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input, input));

        _speciesTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _siteIdentifierTypeLookupServiceMock.Setup(x => x.GetByCodeAsync(HoldingIdentifierType.CPHN.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteIdentifierTypeDocument
            {
                IdentifierId = "6b4ca299-895d-4cdb-95dd-670de71ff328",
                Code = HoldingIdentifierType.CPHN.ToString(),
                Name = HoldingIdentifierType.CPHN.GetDescription()!,
                IsActive = true
            });
    }
}