using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Exceptions;
using KeeperData.Infrastructure.ApiClients;
using KeeperData.Tests.Common.Factories.UseCases;
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
    public async Task GetSamHoldingsAsync_ShouldReturnHoldings_WhenApiReturnsSuccess()
    {
        var id = CphGenerator.GenerateFormattedCph();
        var expectedResponse = MockSamData.GetSamHoldingsResponse(id);

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHoldings,
            new { },
            DataBridgeQueries.SamHoldingsByCph(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse);

        var result = await _client.GetSamHoldingsAsync(id, CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].CPH.Should().Be(id);
    }

    [Fact]
    public async Task GetSamHoldersAsync_ShouldReturnHoldings_WhenApiReturnsSuccess()
    {
        var id = CphGenerator.GenerateFormattedCph();
        var expectedResponse = MockSamData.GetSamHoldersResponse(id);

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHolders,
            new { },
            DataBridgeQueries.SamHoldersByCph(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse);

        var result = await _client.GetSamHoldersAsync(id, CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].CphList.Should().Contain(id);
    }

    [Fact]
    public async Task GetSamHerdsAsync_ShouldReturnHoldings_WhenApiReturnsSuccess()
    {
        var id = CphGenerator.GenerateFormattedCph();
        var partyId = Guid.NewGuid().ToString();
        var expectedResponse = MockSamData.GetSamHerdsResponse(id, partyId);

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamHerds,
            new { },
            DataBridgeQueries.SamHerdsByCph(id));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse);

        var result = await _client.GetSamHerdsAsync(id, CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].CPHH.Should().Be(id);
        result[0].OwnerPartyIdList.Should().Contain(partyId);
        result[0].KeeperPartyIdList.Should().Contain(partyId);
    }

    [Fact]
    public async Task GetSamPartyAsync_ShouldReturnHoldings_WhenApiReturnsSuccess()
    {
        var partyId = Guid.NewGuid().ToString();
        var expectedResponse = MockSamData.GetSamPartyResponse(partyId);

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamParties,
            new { },
            DataBridgeQueries.SamPartyByPartyId(partyId));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse);

        var result = await _client.GetSamPartyAsync(partyId, CancellationToken.None);

        result.Should().NotBeNull();
        result.PARTY_ID.Should().Be(partyId);
    }

    [Fact]
    public async Task GetSamPartiesAsync_ShouldReturnHoldings_WhenApiReturnsSuccess()
    {
        var partyId = Guid.NewGuid().ToString();
        var expectedResponse = MockSamData.GetSamPartiesResponse(partyId);

        var uri = RequestUriUtilities.GetQueryUri(
            DataBridgeApiRoutes.GetSamParties,
            new { },
            DataBridgeQueries.SamPartiesByPartyIds([partyId]));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{DataBridgeApiBaseUrl}/{uri}")
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse);

        var result = await _client.GetSamPartiesAsync([partyId], CancellationToken.None);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].PARTY_ID.Should().Be(partyId);
    }

    [Fact]
    public async Task GetCtsHoldingsAsync_ShouldReturnHoldings_WhenApiReturnsSuccess()
    {
        var id = CphGenerator.GenerateFormattedCph();
        var expectedResponse = MockCtsData.GetCtsHoldingsResponse(id);

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
        var expectedResponse = MockCtsData.GetCtsAgentOrKeeperResponse(id);

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
        var expectedResponse = MockCtsData.GetCtsAgentOrKeeperResponse(id);

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
    public async Task GivenServiceUnavailable_ShouldThrowRetryableException_OnServerError()
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
    public async Task GivenBadRequest_ShouldThrowNonRetryableException_OnClientError()
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
    public async Task GivenTaskCanceledException_ShouldThrowRetryableException_OnTimeout()
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
    public async Task GivenInvalidJsonContent_ShouldThrowNonRetryableException_OnDeserializationError()
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
}