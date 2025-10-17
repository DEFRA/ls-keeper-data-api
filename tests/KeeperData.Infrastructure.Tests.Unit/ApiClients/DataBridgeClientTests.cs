using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Exceptions;
using KeeperData.Infrastructure.ApiClients;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients;

public class DataBridgeClientTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<DataBridgeClient>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly HttpClient _httpClient;
    private readonly DataBridgeClient _client;

    private const string DataBridgeApiBaseUrl = "http://localhost:5560";

    public DataBridgeClientTests()
    {
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(DataBridgeApiBaseUrl)
        };

        _httpClientFactoryMock
            .Setup(f => f.CreateClient("DataBridgeApi"))
            .Returns(_httpClient);

        _client = new DataBridgeClient(_httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_ShouldReturnHoldings_WhenApiReturnsSuccess()
    {
        var id = CphGenerator.GenerateFormattedCph();
        var expectedResponse = GetCtsHoldingsResponse(id);

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsHoldings,
            new { },
            DataBridgeQueries.CtsHoldingsByLidFullIdentifier(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse);

        var result = await _client.GetCtsHoldingsAsync(id, CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].LID_FULL_IDENTIFIER.Should().Be(id);
    }

    [Fact]
    public async Task GetCtsAgentsAsync_ShouldReturnAgents_WhenApiReturnsSuccess()
    {
        var id = CphGenerator.GenerateFormattedCph();
        var expectedResponse = GetCtsAgentOrKeeperResponse(id);

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsAgents,
            new { },
            DataBridgeQueries.CtsAgentsByLidFullIdentifier(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse);

        var result = await _client.GetCtsAgentsAsync(id, CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].LID_FULL_IDENTIFIER.Should().Be(id);
    }

    [Fact]
    public async Task GetCtsKeepersAsync_ShouldReturnKeepers_WhenApiReturnsSuccess()
    {
        var id = CphGenerator.GenerateFormattedCph();
        var expectedResponse = GetCtsAgentOrKeeperResponse(id);

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsKeepers,
            new { },
            DataBridgeQueries.CtsKeepersByLidFullIdentifier(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse);

        var result = await _client.GetCtsKeepersAsync(id, CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].LID_FULL_IDENTIFIER.Should().Be(id);
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_ShouldThrowRetryableException_OnServerError()
    {
        var id = CphGenerator.GenerateFormattedCph();

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsHoldings,
            new { },
            DataBridgeQueries.CtsHoldingsByLidFullIdentifier(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.ServiceUnavailable);

        var act = () => _client.GetCtsHoldingsAsync(id, CancellationToken.None);

        await act.Should().ThrowAsync<RetryableException>()
            .WithMessage("*Transient failure*");
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_ShouldThrowNonRetryableException_OnClientError()
    {
        var id = CphGenerator.GenerateFormattedCph();

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsHoldings,
            new { },
            DataBridgeQueries.CtsHoldingsByLidFullIdentifier(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.BadRequest, new StringContent("Bad request"));

        var act = () => _client.GetCtsHoldingsAsync(id, CancellationToken.None);

        await act.Should().ThrowAsync<NonRetryableException>()
            .WithMessage("*Permanent failure*");
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_ShouldThrowRetryableException_OnTimeout()
    {
        var id = CphGenerator.GenerateFormattedCph();

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsHoldings,
            new { },
            DataBridgeQueries.CtsHoldingsByLidFullIdentifier(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ThrowsAsync(new TaskCanceledException());

        var act = () => _client.GetCtsHoldingsAsync(id, CancellationToken.None);

        await act.Should().ThrowAsync<RetryableException>()
            .WithMessage("*Timeout*");
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_ShouldThrowNonRetryableException_OnDeserializationError()
    {
        var id = CphGenerator.GenerateFormattedCph();

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetCtsHoldings,
            new { },
            DataBridgeQueries.CtsHoldingsByLidFullIdentifier(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, new StringContent("not valid json"));

        var act = () => _client.GetCtsHoldingsAsync(id, CancellationToken.None);

        await act.Should().ThrowAsync<NonRetryableException>()
            .WithMessage("*Deserialization error*");
    }

    private static StringContent GetCtsHoldingsResponse(string holdingIdentifier)
    {
        var mockHolding = new MockCtsDataFactory().CreateMockHolding(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: holdingIdentifier);

        var expectedModels = new List<CtsCphHolding> { mockHolding };
        var expectedResponse = HttpContentUtility.CreateResponseContent(expectedModels);

        return expectedResponse;
    }

    private static StringContent GetCtsAgentOrKeeperResponse(string holdingIdentifier)
    {
        var mockAgentOrKeeper = new MockCtsDataFactory().CreateMockAgentOrKeeper(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: holdingIdentifier);

        var expectedModels = new List<CtsAgentOrKeeper> { mockAgentOrKeeper };
        var expectedResponse = HttpContentUtility.CreateResponseContent(expectedModels);

        return expectedResponse;
    }
}