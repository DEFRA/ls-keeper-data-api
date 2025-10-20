using FluentAssertions;
using KeeperData.Application.Orchestration.Cts.Inserts;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Cts.Inserts;

public class CtsHoldingInsertOrchestratorTests
{
    [Fact]
    public async Task GivenAHoldingIdentifier_WhenExecutingCtsHoldingInsertOrchestrator_ShouldProcessAllStepsSuccessfully()
    {
        var (holdingIdentifier, holdings, agents, keepers) = new MockCtsDataFactory().CreateMockData(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            holdingCount: 1,
            agentCount: 1,
            keeperCount: 1);

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

        var factory = new AppWebApplicationFactory();

        SetupDataBridgeApiRequest(factory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(holdings));
        SetupDataBridgeApiRequest(factory, agentsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(agents));
        SetupDataBridgeApiRequest(factory, keepersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(keepers));

        var result = await ExecuteTestAsync(factory, holdingIdentifier);

        VerifyDataBridgeApiEndpointCalled(factory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, agentsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, keepersUri, Times.Once());

        result.RawHoldings.Should().NotBeNull().And.HaveCount(1);
        result.RawHoldings[0].LID_FULL_IDENTIFIER.Should().Be(holdingIdentifier);

        result.RawAgents.Should().NotBeNull().And.HaveCount(1);
        result.RawAgents[0].LID_FULL_IDENTIFIER.Should().Be(holdingIdentifier);

        result.RawKeepers.Should().NotBeNull().And.HaveCount(1);
        result.RawKeepers[0].LID_FULL_IDENTIFIER.Should().Be(holdingIdentifier);
    }

    private static async Task<CtsHoldingInsertContext> ExecuteTestAsync(AppWebApplicationFactory factory, string holdingIdentifier)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new CtsHoldingInsertContext
        {
            Cph = holdingIdentifier,
            BatchId = 1
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<CtsHoldingInsertOrchestrator>();
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
}