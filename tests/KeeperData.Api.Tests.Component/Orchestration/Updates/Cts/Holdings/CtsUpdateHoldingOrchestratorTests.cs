using AutoFixture;
using FluentAssertions;
using KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Mocks;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Orchestration.Updates.Cts.Holdings;

public class CtsUpdateHoldingOrchestratorTests : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture;
    private readonly Fixture _fixture;

    public CtsUpdateHoldingOrchestratorTests(AppTestFixture appTestFixture)
    {
        _appTestFixture = appTestFixture;
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenAHoldingCph_WhenExecutingCtsUpdateHoldingOrchestrator_ShouldProcessAllStepsSuccessfully()
    {
        // Arrange
        var mockCtsDataFactory = new MockCtsRawDataFactory();
        var cph = "AH-012345-01";
        var mockHolding = mockCtsDataFactory.CreateMockHolding(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: cph);

        var mockAgent = mockCtsDataFactory.CreateMockAgentOrKeeper(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: cph);

        var mockKeeper = mockCtsDataFactory.CreateMockAgentOrKeeper(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: cph);

        var (holdingsUri, agentsUri, keepersUri) = GetAllQueryUris(cph);

        SetupRepositoryMocks(0, 0);
        SetupRoleRepositoryMock();

        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, holdingsUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope([mockHolding]));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, agentsUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope([mockAgent]));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, keepersUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope([mockKeeper]));

        // Act
        var result = await ExecuteTestAsync(_appTestFixture.AppWebApplicationFactory, cph);

        // Assert
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, agentsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, keepersUri, Times.Once());

        VerifyRawDataTypes(result, cph);
        VerifySilverDataTypes(result, cph.LidIdentifierToCph());
    }

    [Fact]
    public async Task GivenAHoldingCph_WhenExecutingCtsUpdateHoldingOrchestrator_WithExistingData_ShouldProcessUpdate()
    {
        // Arrange
        var mockCtsDataFactory = new MockCtsRawDataFactory();
        var cph = "AH-012345-02";
        var mockHolding = mockCtsDataFactory.CreateMockHolding(
            changeType: DataBridgeConstants.ChangeTypeUpdate,
            batchId: 1,
            holdingIdentifier: cph);

        var mockAgent = mockCtsDataFactory.CreateMockAgentOrKeeper(
            changeType: DataBridgeConstants.ChangeTypeUpdate,
            batchId: 1,
            holdingIdentifier: cph);

        var (holdingsUri, agentsUri, keepersUri) = GetAllQueryUris(cph);

        SetupRepositoryMocks(1, 1);
        SetupRoleRepositoryMock();

        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, holdingsUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope([mockHolding]));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, agentsUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope([mockAgent]));
        SetupDataBridgeApiRequest(_appTestFixture.AppWebApplicationFactory, keepersUri, HttpStatusCode.OK,
            HttpContentUtility.CreateResponseContentWithEnvelope<CtsAgentOrKeeper>([]));

        // Act
        var result = await ExecuteTestAsync(_appTestFixture.AppWebApplicationFactory, cph);

        // Assert
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, holdingsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, agentsUri, Times.Once());
        VerifyDataBridgeApiEndpointCalled(_appTestFixture.AppWebApplicationFactory, keepersUri, Times.Once());

        VerifyRawDataTypes(result, cph);
    }

    private static async Task<CtsUpdateHoldingContext> ExecuteTestAsync(AppWebApplicationFactory factory, string cph)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new CtsUpdateHoldingContext
        {
            Cph = cph,
            CurrentDateTime = DateTime.UtcNow
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<CtsUpdateHoldingOrchestrator>();
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

    private static void VerifyRawDataTypes(CtsUpdateHoldingContext context, string cph)
    {
        context.RawHolding?.Should().NotBeNull();
        if (context.RawHolding != null)
        {
            context.RawHolding.LID_FULL_IDENTIFIER.Should().Be(cph);
        }

        context.RawAgents.Should().NotBeNull();
        if (context.RawAgents.Count > 0)
        {
            context.RawAgents[0].LID_FULL_IDENTIFIER.Should().Be(cph);
        }

        context.RawKeepers.Should().NotBeNull();
        if (context.RawKeepers.Count > 0)
        {
            context.RawKeepers[0].LID_FULL_IDENTIFIER.Should().Be(cph);
        }
    }

    private static void VerifySilverDataTypes(CtsUpdateHoldingContext context, string cphTrimmed)
    {
        if (context.SilverHolding != null)
        {
            context.SilverHolding.Should().NotBeNull();
            context.SilverHolding.CountyParishHoldingNumber.Should().Be(cphTrimmed);
        }

        context.SilverParties.Should().NotBeNull();
        if (context.SilverParties.Count > 0)
        {
            context.SilverParties[0].CountyParishHoldingNumber.Should().Be(cphTrimmed);
        }

        context.SilverPartyRoles.Should().NotBeNull();
        if (context.SilverPartyRoles.Count > 0)
        {
            context.SilverPartyRoles[0].HoldingIdentifier.Should().Be(cphTrimmed);
        }
    }

    private static (string holdingsUri, string agentsUri, string keepersUri) GetAllQueryUris(string cph)
    {
        var holdingsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsHoldings,
            new { },
            DataBridgeQueries.CtsHoldingsByLidFullIdentifier(cph));

        var agentsUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsAgents,
            new { },
            DataBridgeQueries.CtsAgentsByLidFullIdentifier(cph));

        var keepersUri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsKeepers,
            new { },
            DataBridgeQueries.CtsKeepersByLidFullIdentifier(cph));

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

        CommonRepositoryMocks.SetupDefaultCtsSilverRepositoryMocks(_appTestFixture.AppWebApplicationFactory);

        _appTestFixture.AppWebApplicationFactory._silverCtsHoldingRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<CtsHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHolding);

        _appTestFixture.AppWebApplicationFactory._silverCtsPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(existingParties.ToList()));
    }

    private void SetupRoleRepositoryMock()
    {
        _appTestFixture.AppWebApplicationFactory._roleRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("8184ae3d-c3c4-4904-b1b8-539eeadbf245", "AGENT", "Agent"));
    }
}