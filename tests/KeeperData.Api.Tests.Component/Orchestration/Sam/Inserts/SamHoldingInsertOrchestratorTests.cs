using FluentAssertions;
using KeeperData.Application.Orchestration.Sam.Inserts;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Sam.Inserts;

public class SamHoldingInsertOrchestratorTests
{
    [Fact]
    public async Task GivenAHoldingIdentifier_WhenExecutingSamHoldingInsertOrchestrator_ShouldProcessAllStepsSuccessfully()
    {
        var (holdingIdentifier, holdings, holders, herds, parties) = new MockSamDataFactory().CreateMockData(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            holdingCount: 1,
            holderCount: 1,
            herdCount: 1,
            partyCount: 1);

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
            DataBridgeQueries.SamPartiesByPartyIds(parties.Select(x => x.PARTY_ID)));

        var factory = new AppWebApplicationFactory();

        SetupDataBridgeApiRequest(factory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(holdings));
        SetupDataBridgeApiRequest(factory, holdersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(holders));
        SetupDataBridgeApiRequest(factory, herdsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(herds));
        SetupDataBridgeApiRequest(factory, partiesUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContent(parties));

        var result = await ExecuteTestAsync(factory, holdingIdentifier);

        VerifyDataBridgeApiEndpointCalled(factory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, holdersUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, herdsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(factory, partiesUri, Times.Once());

        result.RawHoldings.Should().NotBeNull().And.HaveCount(1);
        result.RawHoldings[0].CPH.Should().Be(holdingIdentifier);

        result.RawHolders.Should().NotBeNull().And.HaveCount(1);
        result.RawHolders[0].CphList.Should().NotBeNull().And.Contain(holdingIdentifier);

        result.RawHerds.Should().NotBeNull().And.HaveCount(1);
        result.RawHerds[0].CPHH.Should().Be(holdingIdentifier);

        result.RawParties.Should().NotBeNull().And.HaveCount(1);
        result.RawParties[0].PARTY_ID.Should().Be(parties[0].PARTY_ID);
    }

    private static async Task<SamHoldingInsertContext> ExecuteTestAsync(AppWebApplicationFactory factory, string holdingIdentifier)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new SamHoldingInsertContext
        {
            Cph = holdingIdentifier,
            BatchId = 1
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<SamHoldingInsertOrchestrator>();
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