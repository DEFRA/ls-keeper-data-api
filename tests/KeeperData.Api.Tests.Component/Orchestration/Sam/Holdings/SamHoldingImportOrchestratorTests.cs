using AutoFixture;
using FluentAssertions;
using KeeperData.Application.Orchestration.Sam.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
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

namespace KeeperData.Api.Tests.Component.Orchestration.Sam.Holdings;

public class SamHoldingImportOrchestratorTests
{
    private readonly Mock<IGenericRepository<SamHoldingDocument>> _silverHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<PartyRoleRelationshipDocument>> _silverPartyRoleRelationshipRepositoryMock = new();

    private readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();
    private readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();

    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();
    private readonly Mock<IPremiseActivityTypeLookupService> _premiseActivityTypeLookupServiceMock = new();
    private readonly Mock<IPremiseTypeLookupService> _premiseTypeLookupServiceMock = new();
    private readonly Mock<IProductionTypeLookupService> _productionTypeLookupServiceMock = new();
    private readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();

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

        var (holdingsUri, holdersUri, herdsUri, partiesUri) = GetAllQueryUris(holdingIdentifier, parties.Select(x => x.PARTY_ID));

        SetupRepositoryMocks();
        SetupLookupServiceMocks();

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsScoped(_silverHoldingRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverPartyRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverPartyRoleRelationshipRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldSiteRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_goldPartyRepositoryMock.Object);

        SetupDataBridgeApiRequest(factory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(holdings));
        SetupDataBridgeApiRequest(factory, holdersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(holders));
        SetupDataBridgeApiRequest(factory, herdsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(herds));
        SetupDataBridgeApiRequest(factory, partiesUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(parties));

        var result = await ExecuteTestAsync(factory, holdingIdentifier);

        VerifyDataBridgeApiEndpointCalled(factory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, holdersUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, herdsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, partiesUri, Times.Once());

        VerifyRawDataTypes(result, holdingIdentifier, parties);
        VerifySilverDataTypes(result, holdingIdentifier);
    }

    private static async Task<SamHoldingImportContext> ExecuteTestAsync(AppWebApplicationFactory factory, string holdingIdentifier)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new SamHoldingImportContext
        {
            Cph = holdingIdentifier,
            BatchId = 1
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

    private static void VerifyRawDataTypes(SamHoldingImportContext context, string holdingIdentifier, List<SamParty> parties)
    {
        context.RawHoldings.Should().NotBeNull().And.HaveCount(1);
        context.RawHoldings[0].CPH.Should().Be(holdingIdentifier);

        context.RawHolders.Should().NotBeNull().And.HaveCount(1);
        context.RawHolders[0].CphList.Should().NotBeNull().And.Contain(holdingIdentifier);

        context.RawHerds.Should().NotBeNull().And.HaveCount(1);
        context.RawHerds[0].CPHH.Should().Be(holdingIdentifier);

        context.RawParties.Should().NotBeNull().And.HaveCount(1);
        context.RawParties[0].PARTY_ID.Should().Be(parties[0].PARTY_ID);
    }

    private static void VerifySilverDataTypes(SamHoldingImportContext context, string holdingIdentifier)
    {
        context.SilverHoldings.Should().NotBeNull().And.HaveCount(1);
        context.SilverHoldings[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);
        context.SilverHoldings[0].GroupMarks.Should().NotBeNull().And.HaveCount(1);
        context.SilverHoldings![0].GroupMarks![0].Should().NotBeNull();
        context.SilverHoldings![0].GroupMarks![0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);

        context.SilverParties.Should().NotBeNull().And.HaveCount(2);
        context.SilverParties[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);
        context.SilverParties[1].CountyParishHoldingNumber.Should().Be(holdingIdentifier);

        context.SilverPartyRoles.Should().NotBeNull().And.HaveCount(2);
        context.SilverPartyRoles[0].HoldingIdentifier.Should().Be(holdingIdentifier);
        context.SilverPartyRoles[1].HoldingIdentifier.Should().Be(holdingIdentifier);

        context.SilverHerds.Should().NotBeNull().And.HaveCount(1);
        context.SilverHerds[0].CountyParishHoldingHerd.Should().Be(holdingIdentifier);

        // TODO - Add Gold
    }

    private static (string holdingsUri, string holdersUri, string herdsUri, string partiesUri) GetAllQueryUris(string holdingIdentifier, IEnumerable<string> partyIds)
    {
        var holdingsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHoldings,
            new { },
            DataBridgeQueries.SamHoldingsByCph(holdingIdentifier));

        var holdersUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHolders,
            new { },
            DataBridgeQueries.SamHoldersByCph(holdingIdentifier));

        var herdsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHerds,
            new { },
            DataBridgeQueries.SamHerdsByCph(holdingIdentifier));

        var partiesUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamParties,
            new { },
            DataBridgeQueries.SamPartiesByPartyIds(partyIds));

        return (holdingsUri, holdersUri, herdsUri, partiesUri);
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

        // SamHoldingDocument
        _silverHoldingRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSilverHolding);

        _silverHoldingRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // SamPartyDocument
        _silverPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(existingSilverParties.ToList()));

        _silverPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // PartyRoleRelationshipDocument
        _silverPartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPartyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // SiteDocument
        _goldSiteRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SiteDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGoldSite);

        _goldSiteRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SiteDocument>, SiteDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // PartyDocument
        _goldPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(existingGoldParties.ToList()));

        _goldPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<PartyDocument>, PartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupLookupServiceMocks()
    {
        _countryIdentifierLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

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
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));

        _speciesTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? input, CancellationToken token) => (Guid.NewGuid().ToString(), input));
    }
}