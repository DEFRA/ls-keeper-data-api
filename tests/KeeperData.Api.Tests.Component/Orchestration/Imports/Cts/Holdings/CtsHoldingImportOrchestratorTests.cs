using AutoFixture;
using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Cts.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Repositories;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Imports.Cts.Holdings;

public class CtsHoldingImportOrchestratorTests
{
    private readonly Mock<IGenericRepository<CtsHoldingDocument>> _silverHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<CtsPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();

    private readonly Fixture _fixture;

    public CtsHoldingImportOrchestratorTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenAHoldingIdentifier_WhenExecutingCtsHoldingImportOrchestrator_ShouldProcessAllStepsSuccessfully()
    {
        var (holdingIdentifier, holdings, agents, keepers) = new MockCtsRawDataFactory().CreateMockData(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            holdingCount: 1,
            agentCount: 1,
            keeperCount: 1);

        var (holdingsUri, agentsUri, keepersUri) = GetAllQueryUris(holdingIdentifier);

        SetupRepositoryMocks(1, 2);
        SetupRoleRepositoryMock();

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsScoped(_silverHoldingRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_silverPartyRepositoryMock.Object);
        factory.OverrideServiceAsScoped(_roleRepositoryMock.Object);

        SetupDataBridgeApiRequest(factory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(holdings));
        SetupDataBridgeApiRequest(factory, agentsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(agents));
        SetupDataBridgeApiRequest(factory, keepersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(keepers));

        var result = await ExecuteTestAsync(factory, holdingIdentifier);

        VerifyDataBridgeApiEndpointCalled(factory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, agentsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, keepersUri, Times.Once());

        VerifyRawDataTypes(result, holdingIdentifier);
        VerifySilverDataTypes(result, holdingIdentifier.LidIdentifierToCph());
    }

    private static async Task<CtsHoldingImportContext> ExecuteTestAsync(AppWebApplicationFactory factory, string holdingIdentifier)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new CtsHoldingImportContext
        {
            Cph = holdingIdentifier,
            BatchId = 1,
            CurrentDateTime = DateTime.UtcNow
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<CtsHoldingImportOrchestrator>();
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

    private static void VerifyRawDataTypes(CtsHoldingImportContext context, string holdingIdentifier)
    {
        context.RawHoldings.Should().NotBeNull();
        if (context.RawHoldings.Count > 0)
        {
            context.RawHoldings[0].LID_FULL_IDENTIFIER.Should().Be(holdingIdentifier);
        }

        context.RawAgents.Should().NotBeNull();
        if (context.RawAgents.Count > 0)
        {
            context.RawAgents[0].LID_FULL_IDENTIFIER.Should().Be(holdingIdentifier);
        }

        context.RawKeepers.Should().NotBeNull();
        if (context.RawKeepers.Count > 0)
        {
            context.RawKeepers[0].LID_FULL_IDENTIFIER.Should().Be(holdingIdentifier);
        }
    }

    private static void VerifySilverDataTypes(CtsHoldingImportContext context, string holdingIdentifier)
    {
        context.SilverHoldings.Should().NotBeNull().And.HaveCount(context.RawHoldings.Count);
        context.SilverHoldings[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);

        context.SilverParties.Should().NotBeNull().And.HaveCount(context.RawAgents.Count + context.RawKeepers.Count);
        context.SilverParties[0].CountyParishHoldingNumber.Should().Be(holdingIdentifier);
        context.SilverParties[1].CountyParishHoldingNumber.Should().Be(holdingIdentifier);
        
        context.SilverPartyRoles.Should().NotBeNull().And.HaveCount(context.RawAgents.Count + context.RawKeepers.Count);
        context.SilverPartyRoles[0].HoldingIdentifier.Should().Be(holdingIdentifier);
        context.SilverPartyRoles[1].HoldingIdentifier.Should().Be(holdingIdentifier);
    }

    private static (string holdingsUri, string agentsUri, string keepersUri) GetAllQueryUris(string holdingIdentifier)
    {
        var holdingsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsHoldings,
            new { },
            DataBridgeQueries.CtsHoldingsByLidFullIdentifier(holdingIdentifier));

        var agentsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsAgents,
            new { },
            DataBridgeQueries.CtsAgentsByLidFullIdentifier(holdingIdentifier));

        var keepersUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsKeepers,
            new { },
            DataBridgeQueries.CtsKeepersByLidFullIdentifier(holdingIdentifier));

        return (holdingsUri, agentsUri, keepersUri);
    }

    private void SetupRepositoryMocks(
        int existingHoldingsCount = 0,
        int existingPartiesCount = 0)
    {
        var existingHolding = existingHoldingsCount == 0
            ? null : _fixture.Create<CtsHoldingDocument>();

        var existingParties = existingPartiesCount == 0
            ? []
            : _fixture.CreateMany<CtsPartyDocument>(existingPartiesCount);

        // Holding
        _silverHoldingRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<CtsHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHolding);

        _silverHoldingRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsHoldingDocument>, CtsHoldingDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Party
        _silverPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(existingParties.ToList()));

        _silverPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsPartyDocument>, CtsPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupRoleRepositoryMock()
    {
        // Setup FindAsync to return role data for common roles used in tests
        _roleRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("8184ae3d-c3c4-4904-b1b8-539eeadbf245", "Agent"));
    }
}