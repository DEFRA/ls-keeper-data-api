using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.TestData;
using KeeperData.Tests.Common.TestData.Sam;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Imports.Sam;

public class SamBulkImportWithAccurateRawDataTests
{
    private readonly Mock<IGenericRepository<SamHoldingDocument>> _silverHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<ISilverSitePartyRoleRelationshipRepository> _silverSitePartyRoleRelationshipRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamHerdDocument>> _silverHerdRepositoryMock = new();

    private readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();
    private readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>> _goldSitePartyRoleRelationshipRepositoryMock = new();
    private readonly Mock<IGenericRepository<SiteGroupMarkRelationshipDocument>> _goldSiteGroupMarkRelationshipRepositoryMock = new();

    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    private readonly Mock<IPremiseActivityTypeLookupService> _premiseActivityTypeLookupServiceMock = new();
    private readonly Mock<IPremiseTypeLookupService> _premiseTypeLookupServiceMock = new();
    private readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();

    [Fact]
    public async Task GivenAHoldingIdentifier_WhenExecutingTheBulkImportStrategy_ShouldCorrectlyTransformTheData()
    {
        var scenarioData = SamTestScenarios.DefaultScenario;

        var (holdingsUri, herdsUri, holdersUri, partiesUri) = GetAllQueryUris(scenarioData.Cph, scenarioData.RawParties.Select(x => x.PARTY_ID));

        SetupDefaultRepositoryMocks();
        SetupDefaultLookupServiceMocks();

        var factory = new AppWebApplicationFactory();
        OverrideServiceMocks(factory);

        SetupDataBridgeApiRequest(factory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawHoldings));
        SetupDataBridgeApiRequest(factory, herdsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawHerds));
        SetupDataBridgeApiRequest(factory, holdersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawHolders));
        SetupDataBridgeApiRequest(factory, partiesUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawParties));

        var result = await ExecuteTestAsync(factory, scenarioData.Cph);

        VerifyDataBridgeApiEndpointCalled(factory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, herdsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, holdersUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, partiesUri, Times.Once());

        VerifyGoldSite(result.GoldSite!, scenarioData.ExpectedGoldSite!);
        VerifyGoldParties(result.GoldParties!, scenarioData.ExpectedGoldParties!);
        VerifyGoldSitePartyRoles(result.GoldSitePartyRoles!, scenarioData.ExpectedGoldSitePartyRoles!);
        VerifyGoldSiteGroupMarks(result.GoldSiteGroupMarks!, scenarioData.ExpectedGoldSiteGroupMarks!);
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
        incoming.OrderBy(x => x.PartyId).ThenBy(x => x.RoleTypeName).Should().BeEquivalentTo(
            expected.OrderBy(x => x.PartyId).ThenBy(x => x.RoleTypeName),
            options => options
                .Excluding(ctx => ctx.Id)
                .Excluding(ctx => ctx.Path.EndsWith("LastUpdatedDate"))
        );
    }

    private static void VerifyGoldSiteGroupMarks(
        List<SiteGroupMarkRelationshipDocument> incoming,
        List<SiteGroupMarkRelationshipDocument> expected)
    {
        incoming.OrderBy(x => x.PartyId).ThenBy(x => x.RoleTypeName).Should().BeEquivalentTo(
            expected.OrderBy(x => x.PartyId).ThenBy(x => x.RoleTypeName),
            options => options
                .Excluding(ctx => ctx.Id)
                .Excluding(ctx => ctx.Path.EndsWith("LastUpdatedDate"))
        );
    }

    private void OverrideServiceMocks(AppWebApplicationFactory factory)
    {
        factory.OverrideServiceAsScoped(_silverHoldingRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverPartyRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverSitePartyRoleRelationshipRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverHerdRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldSiteRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldPartyRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldSitePartyRoleRelationshipRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldSiteGroupMarkRelationshipRepositoryMock.Object);

        factory.OverrideServiceAsScoped(_countryIdentifierLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_premiseActivityTypeLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_premiseTypeLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_productionUsageLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_roleTypeLookupServiceMock.Object);
        factory.OverrideServiceAsScoped(_speciesTypeLookupServiceMock.Object);
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

        // Silver Role Relationships
        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.Silver.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(), It.IsAny<CancellationToken>()))
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
            .ReturnsAsync((SiteDocument?)null);

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

        // Gold Site Group Mark Relationships
        _goldSiteGroupMarkRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SiteGroupMarkRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSiteGroupMarkRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SiteGroupMarkRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupDefaultLookupServiceMocks()
    {
        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (CountryData.Find(code!)));

        _countryIdentifierLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (CountryData.GetById(id!)));

        _premiseActivityTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (PremiseActivityTypeData.Find(code!)));

        _premiseActivityTypeLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (PremiseActivityTypeData.GetById(id!)));

        _premiseTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (PremiseTypeData.Find(code!)));

        _premiseTypeLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (PremiseTypeData.GetById(id!)));

        _productionUsageLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (ProductionUsageData.Find(code!)));

        _productionUsageLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (ProductionUsageData.GetById(id!)));

        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (RoleData.Find(code!)));

        _roleTypeLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (RoleData.GetById(id!)));

        _speciesTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? code, CancellationToken token) => (SpeciesData.Find(code!)));

        _speciesTypeLookupServiceMock
            .Setup(x => x.GetByIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? id, CancellationToken token) => (SpeciesData.GetById(id!)));
    }
}
