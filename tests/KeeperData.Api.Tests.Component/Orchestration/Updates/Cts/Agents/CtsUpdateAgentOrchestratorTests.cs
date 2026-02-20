using AutoFixture;
using FluentAssertions;
using KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Mocks;
using KeeperData.Application.Orchestration.Updates.Cts.Agents;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Updates.Cts.Agents;

public class CtsUpdateAgentOrchestratorTests : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture;
    private readonly Fixture _fixture;

    public CtsUpdateAgentOrchestratorTests(AppTestFixture appTestFixture)
    {
        _appTestFixture = appTestFixture;
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenAnAgentPartyId_WhenExecutingCtsUpdateAgentOrchestrator_ShouldProcessAllStepsSuccessfully()
    {
        // Arrange
        var mockCtsDataFactory = new MockCtsRawDataFactory();
        var partyId = PersonGenerator.GetPartyIds(1)[0];
        var cph = CphGenerator.GenerateFormattedCph();

        var mockAgent = mockCtsDataFactory.CreateMockAgentOrKeeper(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: cph);
        mockAgent.PAR_ID = partyId;

        var agentUri = GetAgentQueryUri(partyId);

        SetupRepositoryMocks(0);
        SetupRoleRepositoryMock();

        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, agentUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope([mockAgent]));

        // Act
        var result = await ExecuteTestAsync(_appTestFixture.AppWebApplicationFactory, partyId);

        // Assert
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, agentUri, Times.Once());

        VerifyRawDataTypes(result, partyId);
        VerifySilverDataTypes(result, partyId);
    }

    [Fact]
    public async Task GivenAnAgentPartyId_WhenExecutingCtsUpdateAgentOrchestrator_WithExistingData_ShouldProcessUpdate()
    {
        // Arrange
        var mockCtsDataFactory = new MockCtsRawDataFactory();
        var partyId = PersonGenerator.GetPartyIds(1)[0];
        var cph = CphGenerator.GenerateFormattedCph();

        var mockAgent = mockCtsDataFactory.CreateMockAgentOrKeeper(
            changeType: DataBridgeConstants.ChangeTypeUpdate,
            batchId: 1,
            holdingIdentifier: cph);
        mockAgent.PAR_ID = partyId;

        var agentUri = GetAgentQueryUri(partyId);

        SetupRepositoryMocks(1);
        SetupRoleRepositoryMock();

        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, agentUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope([mockAgent]));

        // Act
        var result = await ExecuteTestAsync(_appTestFixture.AppWebApplicationFactory, partyId);

        // Assert
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, agentUri, Times.Once());

        VerifyRawDataTypes(result, partyId);
    }

    [Fact]
    public async Task GivenAnAgentPartyId_WhenAgentNotFound_ShouldHandleNullResponse()
    {
        // Arrange
        var partyId = PersonGenerator.GetPartyIds(1)[0];
        var agentUri = GetAgentQueryUri(partyId);

        SetupRepositoryMocks(0);

        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, agentUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope<CtsAgentOrKeeper>([]));

        // Act
        var result = await ExecuteTestAsync(_appTestFixture.AppWebApplicationFactory, partyId);

        // Assert
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, agentUri, Times.Once());

        result.RawAgent.Should().BeNull();
        result.SilverParty.Should().BeNull();
    }

    private static async Task<CtsUpdateAgentContext> ExecuteTestAsync(AppWebApplicationFactory factory, string partyId)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new CtsUpdateAgentContext
        {
            PartyId = partyId,
            CurrentDateTime = DateTime.UtcNow
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<CtsUpdateAgentOrchestrator>();
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

    private static void VerifyRawDataTypes(CtsUpdateAgentContext context, string partyId)
    {
        context.RawAgent.Should().NotBeNull();
        context.RawAgent!.PAR_ID.Should().Be(partyId);
    }

    private static void VerifySilverDataTypes(CtsUpdateAgentContext context, string partyId)
    {
        context.SilverParty.Should().NotBeNull();
        context.SilverParty!.PartyId.Should().Be(partyId);

        context.SilverPartyRoles.Should().NotBeNull();
        if (context.SilverPartyRoles.Count > 0)
        {
            context.SilverPartyRoles[0].PartyId.Should().Be(partyId);
        }
    }

    private static string GetAgentQueryUri(string partyId)
    {
        var agentUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsAgents,
            new { },
            DataBridgeQueries.CtsAgentByPartyId(partyId));

        return agentUri;
    }

    private void SetupRepositoryMocks(int existingPartiesCount = 0)
    {
        var existingParties = existingPartiesCount == 0
            ? null : _fixture.Create<CtsPartyDocument>();

        CommonRepositoryMocks.SetupDefaultCtsSilverRepositoryMocks(_appTestFixture.AppWebApplicationFactory);

        _appTestFixture.AppWebApplicationFactory._silverCtsPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParties);
    }

    private void SetupRoleRepositoryMock()
    {
        _appTestFixture.AppWebApplicationFactory._roleRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("8184ae3d-c3c4-4904-b1b8-539eeadbf245", "AGENT", "Agent"));
    }
}