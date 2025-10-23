using AutoFixture;
using FluentAssertions;
using KeeperData.Application.Orchestration.Sam.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Repositories;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Sam.Holdings;

public class SamHoldingImportOrchestratorTests
{
    private readonly Mock<IGenericRepository<PartyRoleRelationshipDocument>> _partyRoleRelationshipRepositoryMock = new();

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

        VerifyRawDataTypes(result, holdingIdentifier, parties);
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

    private void SetupRepositoryMocks()
    {
        // PartyRoleRelationshipDocuments
        _partyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _partyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}