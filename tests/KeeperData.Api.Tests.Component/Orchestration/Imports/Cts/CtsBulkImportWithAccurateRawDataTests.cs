using FluentAssertions;
using KeeperData.Api.Tests.Component.Orchestration.Imports.Sam.Mocks;
using KeeperData.Application.Orchestration.Imports.Cts.Holdings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Tests.Common.TestData.Cts;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Moq.Contrib.HttpClient;
using System.Linq.Expressions;
using System.Net;
using static KeeperData.Tests.Common.TestData.Cts.CtsTestScenarios;

namespace KeeperData.Api.Tests.Component.Orchestration.Imports.Cts;

public class CtsBulkImportWithAccurateRawDataTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    [Fact]
    public async Task GivenAHoldingIdentifier_WhenExecutingCtsBulkImportStrategy_ShouldCorrectlyProcessRawData()
    {
        // Arrange
        var scenarioData = CtsTestScenarios.DefaultScenario;

        // Act
        var result = await RunDefaultScenarioAsync(scenarioData);

        // Assert
        VerifyRawDataLoaded(result, scenarioData);
        VerifySilverDataCreated(result);
    }

    [Fact]
    public async Task GivenHoldingDataAlreadyExists_WhenTheAgentIsChanged_ShouldCorrectlyUpdateData()
    {
        // Arrange
        var importScenarioData = CtsTestScenarios.DefaultScenario;
        var importResult = await RunDefaultScenarioAsync(importScenarioData);

        var updateScenarioData = CtsTestScenarios.Scenario_UpdatedAgentAndKeepers();

        // Act
        var updateResult = await RunDefaultScenarioAsync(updateScenarioData, importResult);

        // Assert
        VerifyRawDataLoaded(updateResult, updateScenarioData);
        VerifySilverDataCreated(updateResult);
    }

    private async Task<CtsHoldingImportContext> RunDefaultScenarioAsync(CtsTestScenarioData scenarioData, CtsHoldingImportContext? existingContext = null)
    {
        var (holdingsUri, agentsUri, keepersUri) = GetAllQueryUris(scenarioData.Cph);

        SetupDefaultRepositoryMocks();
        SetupRoleRepositoryMock();

        if (existingContext != null)
        {
            SetupRepositoryMocksFromContext(existingContext);
        }

        SetupDataBridgeApiRequest(appTestFixture.AppWebApplicationFactory, holdingsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawHoldings));
        SetupDataBridgeApiRequest(appTestFixture.AppWebApplicationFactory, agentsUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawAgents));
        SetupDataBridgeApiRequest(appTestFixture.AppWebApplicationFactory, keepersUri, HttpStatusCode.OK, HttpContentUtility.CreateResponseContentWithEnvelope(scenarioData.RawKeepers));

        return await ExecuteTestAsync(appTestFixture.AppWebApplicationFactory, scenarioData.Cph);
    }

    private static async Task<CtsHoldingImportContext> ExecuteTestAsync(AppWebApplicationFactory factory, string holdingIdentifier)
    {
        var mockPoller = new Mock<IQueuePoller>();
        factory.OverrideServiceAsSingleton(mockPoller.Object);

        var context = new CtsHoldingImportContext
        {
            Cph = holdingIdentifier,
            BatchId = 1001,
            CurrentDateTime = DateTime.Parse("2024-01-15T10:30:00Z")
        };

        using var scope = factory.Services.CreateAsyncScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<CtsHoldingImportOrchestrator>();
        await orchestrator.ExecuteAsync(context, CancellationToken.None);

        return context;
    }

    private static void VerifyRawDataLoaded(CtsHoldingImportContext context, CtsTestScenarioData expectedData)
    {
        context.RawHoldings.Should().HaveCount(expectedData.RawHoldings.Count);
        if (expectedData.RawHoldings.Any())
        {
            var actualHolding = context.RawHoldings.First();
            var expectedHolding = expectedData.RawHoldings.First();

            actualHolding.LID_FULL_IDENTIFIER.Should().Be(expectedHolding.LID_FULL_IDENTIFIER);
            actualHolding.LTY_LOC_TYPE.Should().Be(expectedHolding.LTY_LOC_TYPE);
            actualHolding.ADR_NAME.Should().Be(expectedHolding.ADR_NAME);
        }

        context.RawAgents.Should().HaveCount(expectedData.RawAgents.Count);
        if (expectedData.RawAgents.Any())
        {
            var actualAgent = context.RawAgents.First();
            var expectedAgent = expectedData.RawAgents.First();

            actualAgent.PAR_ID.Should().Be(expectedAgent.PAR_ID);
            actualAgent.LID_FULL_IDENTIFIER.Should().Be(expectedAgent.LID_FULL_IDENTIFIER);
            actualAgent.PAR_SURNAME.Should().Be(expectedAgent.PAR_SURNAME);
        }

        context.RawKeepers.Should().HaveCount(expectedData.RawKeepers.Count);
        if (expectedData.RawKeepers.Any())
        {
            var actualKeeper = context.RawKeepers.First();
            var expectedKeeper = expectedData.RawKeepers.First();

            actualKeeper.PAR_ID.Should().Be(expectedKeeper.PAR_ID);
            actualKeeper.LID_FULL_IDENTIFIER.Should().Be(expectedKeeper.LID_FULL_IDENTIFIER);
            actualKeeper.PAR_SURNAME.Should().Be(expectedKeeper.PAR_SURNAME);
        }
    }

    private static void VerifySilverDataCreated(CtsHoldingImportContext context)
    {
        context.SilverHoldings.Should().NotBeEmpty("Silver holdings should be created");
        context.SilverParties.Should().NotBeEmpty("Silver parties should be created");
        context.SilverPartyRoles.Should().NotBeEmpty("Silver party roles should be created");

        var silverHolding = context.SilverHoldings.First();
        silverHolding.Should().NotBeNull();
        silverHolding.CountyParishHoldingNumber.Should().NotBeEmpty();
        silverHolding.LastUpdatedBatchId.Should().BeGreaterThan(0);

        var silverParties = context.SilverParties;
        silverParties.Should().HaveCountGreaterOrEqualTo(2, "Should have at least agent and keeper parties");

        foreach (var party in silverParties)
        {
            party.PartyId.Should().NotBeEmpty();
            party.LastUpdatedBatchId.Should().BeGreaterThan(0);
        }

        var silverRoles = context.SilverPartyRoles;
        silverRoles.Should().HaveCountGreaterOrEqualTo(2, "Should have at least agent and keeper roles");

        foreach (var role in silverRoles)
        {
            role.HoldingIdentifier.Should().NotBeEmpty();
            role.PartyId.Should().NotBeEmpty();
            role.LastUpdatedBatchId.Should().BeGreaterThan(0);
        }
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

    private void SetupDefaultRepositoryMocks()
    {
        CommonRepositoryMocks.SetupDefaultCtsSilverRepositoryMocks(appTestFixture.AppWebApplicationFactory);
    }

    private void SetupRoleRepositoryMock()
    {
        appTestFixture.AppWebApplicationFactory._roleRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("8184ae3d-c3c4-4904-b1b8-539eeadbf245", "AGENT", "Agent"));
    }

    private void SetupRepositoryMocksFromContext(CtsHoldingImportContext existingContext)
    {
        if (existingContext.SilverHoldings?.Any() == true)
        {
            appTestFixture.AppWebApplicationFactory._silverCtsHoldingRepositoryMock
                .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<CtsHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingContext.SilverHoldings.First());
        }

        if (existingContext.SilverParties?.Any() == true)
        {
            appTestFixture.AppWebApplicationFactory._silverCtsPartyRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(existingContext.SilverParties.ToList()));
        }
    }

    private static void SetupDataBridgeApiRequest(
        AppWebApplicationFactory factory,
        string uri,
        HttpStatusCode statusCode,
        StringContent content)
    {
        factory.DataBridgeApiClientHttpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TestConstants.DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(statusCode, content);
    }
}