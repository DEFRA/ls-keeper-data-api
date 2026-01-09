using FluentAssertions;
using KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Mocks;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Tests.Common.TestData;
using KeeperData.Tests.Common.TestData.Sam;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;
using static KeeperData.Tests.Common.TestData.Sam.SamTestScenarios;

namespace KeeperData.Api.Tests.Component.Orchestration.Imports.Sam;

public class SamBulkImportWithAccurateRawDataTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    [Fact]
    public async Task GivenAHoldingIdentifier_WhenExecutingTheBulkImportStrategy_ShouldCorrectlyTransformTheData()
    {
        var scenarioData = SamTestScenarios.DefaultScenario;
        var result = await RunDefaultScenarioAsync(scenarioData);

        VerifyGoldSite(result.GoldSite!, scenarioData.ExpectedGoldSite!);
        VerifyGoldParties(result.GoldParties!, scenarioData.ExpectedGoldParties!);
        VerifyGoldSitePartyRoles(result.GoldSitePartyRoles!, scenarioData.ExpectedGoldSitePartyRoles!);
        VerifyGoldSiteGroupMarks(result.GoldSiteGroupMarks!, scenarioData.ExpectedGoldSiteGroupMarks!);
    }

    [Fact]
    public async Task GivenHoldingDataAlreadyExists_WhenTheHolderIsChanged_ShouldCorrectlyTransformTheData()
    {
        var importScenarioData = SamTestScenarios.DefaultScenario;
        var importResult = await RunDefaultScenarioAsync(importScenarioData);

        var updateScenarioData = SamTestScenarios.Scenario_UpdatedHolderAndParties();
        var updateResult = await RunDefaultScenarioAsync(updateScenarioData, importResult);

        VerifyGoldSite(updateResult.GoldSite!, updateScenarioData.ExpectedGoldSite!);
        VerifyGoldParties(updateResult.GoldParties!, updateScenarioData.ExpectedGoldParties!);
        VerifyGoldSitePartyRoles(updateResult.GoldSitePartyRoles!, updateScenarioData.ExpectedGoldSitePartyRoles!);
        VerifyGoldSiteGroupMarks(updateResult.GoldSiteGroupMarks!, updateScenarioData.ExpectedGoldSiteGroupMarks!);
    }

    private async Task<SamHoldingImportContext> RunDefaultScenarioAsync(SamTestScenarioData scenarioData, SamHoldingImportContext? existingContext = null)
    {
        var partyIds = scenarioData.RawParties.Select(x => x.PARTY_ID).Union(scenarioData.RawHolders.Select(x => x.PARTY_ID)).Distinct().ToList();

        var (holdingsUri, herdsUri, holdersUri, partiesUri) = GetAllQueryUris(scenarioData.Cph, partyIds);

        SetupDefaultRepositoryMocks();
        SetupDefaultLookupServiceMocks();

        if (existingContext != null)
        {
            SetupRepositoryMocksFromContext(existingContext);
        }

        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawHoldings));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, herdsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawHerds));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, holdersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawHolders));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, partiesUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawParties));

        return await ExecuteTestAsync(_appTestFixture.AppWebApplicationFactory, scenarioData.Cph);
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

    private static void VerifyGoldSite(
        SiteDocument incoming,
        SiteDocument expected)
    {
        incoming.Should().BeEquivalentTo(
            expected,
            options => options
                .Excluding(ctx => ctx.Id)
                .Excluding(ctx => ctx.Path.EndsWith("IdentifierId"))
                .Excluding(ctx => ctx.Path.EndsWith("LastUpdatedDate"))
        );
    }

    private static void VerifyGoldParties(
        List<PartyDocument> incoming,
        List<PartyDocument> expected)
    {
        incoming.OrderBy(x => x.Id).Should().BeEquivalentTo(
            expected.OrderBy(x => x.Id),
            options => options
                .Excluding(ctx => ctx.Id)
                .Excluding(ctx => ctx.Path.EndsWith("IdentifierId"))
                .Excluding(ctx => ctx.Path.EndsWith("LastUpdatedDate"))
        );
    }

    private static void VerifyGoldSitePartyRoles(
        List<Core.Documents.SitePartyRoleRelationshipDocument> incoming,
        List<Core.Documents.SitePartyRoleRelationshipDocument> expected)
    {
        incoming.OrderBy(x => x.CustomerNumber).ThenBy(x => x.RoleTypeName).Should().BeEquivalentTo(
            expected.OrderBy(x => x.CustomerNumber).ThenBy(x => x.RoleTypeName),
            options => options
                .Excluding(ctx => ctx.Id)
                .Excluding(ctx => ctx.Path.EndsWith("LastUpdatedDate"))
        );
    }

    private static void VerifyGoldSiteGroupMarks(
        List<SiteGroupMarkRelationshipDocument> incoming,
        List<SiteGroupMarkRelationshipDocument> expected)
    {
        incoming.OrderBy(x => x.CustomerNumber).ThenBy(x => x.RoleTypeName).Should().BeEquivalentTo(
            expected.OrderBy(x => x.CustomerNumber).ThenBy(x => x.RoleTypeName),
            options => options
                .Excluding(ctx => ctx.Id)
                .Excluding(ctx => ctx.Path.EndsWith("LastUpdatedDate"))
        );
    }

    private static void SetupDataBridgeApiRequest(AppWebApplicationFactory factory, string uri, HttpStatusCode httpStatusCode, StringContent httpResponseMessage)
    {
        factory.DataBridgeApiClientHttpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TestConstants.DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(httpStatusCode, httpResponseMessage);
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

    private void SetupDefaultRepositoryMocks()
    {
        // Silver
        CommonRepositoryMocks.SetupDefaultSamSilverRepositoryMocks(_appTestFixture.AppWebApplicationFactory);

        // Gold
        CommonRepositoryMocks.SetupDefaultGoldRepositoryMocks(_appTestFixture.AppWebApplicationFactory);
    }

    private void SetupRepositoryMocksFromContext(SamHoldingImportContext context)
    {
        // Silver Holding
        _appTestFixture.AppWebApplicationFactory._silverSamHoldingRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. context.SilverHoldings.Select(h =>
            {
                h.Id = Guid.NewGuid().ToString();
                return h;
            })]);

        // Silver Party
        _appTestFixture.AppWebApplicationFactory._silverSamPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. context.SilverParties.Select(h =>
            {
                h.Id = Guid.NewGuid().ToString();
                return h;
            })]);

        // Silver Herds
        _appTestFixture.AppWebApplicationFactory._silverSamHerdRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHerdDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. context.SilverHerds.Select(h =>
            {
                h.Id = Guid.NewGuid().ToString();
                return h;
            })]);

        // Gold Site
        _appTestFixture.AppWebApplicationFactory._goldSiteRepositoryMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                context.GoldSite!.Id = Guid.NewGuid().ToString();
                return context.GoldSite;
            });

        // Gold Party
        _appTestFixture.AppWebApplicationFactory._goldPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<PartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PartyDocument, bool>> expr, CancellationToken _) =>
            {
                var compiled = expr.Compile();
                return context.GoldParties.Select(h =>
                {
                    h.Id = Guid.NewGuid().ToString();
                    return h;
                })
                .ToList().FirstOrDefault(p => compiled(p));
            });

        // Gold Site Party Role Relationships
        _appTestFixture.AppWebApplicationFactory._goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. context.GoldSitePartyRoles.Select(h =>
            {
                h.Id = Guid.NewGuid().ToString();
                return h;
            })]);
    }

    private void SetupDefaultLookupServiceMocks()
    {
        _appTestFixture.AppWebApplicationFactory._countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, string? internalCode, CancellationToken token) => (CountryData.Find(code!, internalCode)));

        _appTestFixture.AppWebApplicationFactory._countryIdentifierLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (CountryData.GetById(id!)));

        _appTestFixture.AppWebApplicationFactory._premiseActivityTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (PremiseActivityTypeData.Find(code!)));

        _appTestFixture.AppWebApplicationFactory._premiseActivityTypeLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (PremiseActivityTypeData.GetById(id!)));

        _appTestFixture.AppWebApplicationFactory._premiseActivityTypeLookupServiceMock.Setup(x => x.GetByCodeAsync("WM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PremisesActivityTypeDocument
            {
                IdentifierId = "e0dd8921-3593-4e58-b797-a7c8673d8e40",
                Code = "WM",
                Name = "White Meat",
                IsActive = true
            });

        _appTestFixture.AppWebApplicationFactory._premiseActivityTypeLookupServiceMock.Setup(x => x.GetByCodeAsync("RM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PremisesActivityTypeDocument
            {
                IdentifierId = "d2d9be5e-18b4-4424-b196-fd40f3b105d8",
                Code = "RM",
                Name = "Red Meat",
                IsActive = true
            });

        _appTestFixture.AppWebApplicationFactory._premiseTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (PremiseTypeData.Find(code!)));

        _appTestFixture.AppWebApplicationFactory._premiseTypeLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (PremiseTypeData.GetById(id!)));

        _appTestFixture.AppWebApplicationFactory._productionUsageLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (ProductionUsageData.Find(code!)));

        _appTestFixture.AppWebApplicationFactory._productionUsageLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (ProductionUsageData.GetById(id!)));

        _appTestFixture.AppWebApplicationFactory._roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (RoleData.Find(code!)));

        _appTestFixture.AppWebApplicationFactory._roleTypeLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (RoleData.GetById(id!)));

        _appTestFixture.AppWebApplicationFactory._speciesTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (SpeciesData.Find(code!)));

        _appTestFixture.AppWebApplicationFactory._speciesTypeLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (SpeciesData.GetById(id!)));

        _appTestFixture.AppWebApplicationFactory._siteIdentifierTypeLookupServiceMock.Setup(x => x.GetByCodeAsync(HoldingIdentifierType.CPHN.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteIdentifierTypeDocument
            {
                IdentifierId = "6b4ca299-895d-4cdb-95dd-670de71ff328",
                Code = HoldingIdentifierType.CPHN.ToString(),
                Name = HoldingIdentifierType.CPHN.GetDescription()!,
                IsActive = true
            });
    }
}