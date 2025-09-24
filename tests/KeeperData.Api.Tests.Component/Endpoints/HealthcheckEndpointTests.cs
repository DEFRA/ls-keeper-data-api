using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class HealthcheckEndpointTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    private const string DataBridgeApiHealthEndpoint = $"{TestConstants.DataBridgeApiBaseUrl}{TestConstants.HealthCheckEndpoint}";

    [Fact]
    public async Task GivenValidHealthCheckRequest_ShouldSucceed()
    {
        _appTestFixture.DataBridgeApiClientHttpMessageHandlerMock.Reset();
        SetupDataBridgeApiHealthCheckRequest(DataBridgeApiHealthEndpoint, HttpStatusCode.OK);

        var client = _appTestFixture.AppWebApplicationFactory.Services
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("DataBridgeApi");

        Console.WriteLine($"BaseAddress: {client.BaseAddress}");


        var response = await _appTestFixture.HttpClient.GetAsync("health");
        var responseBody = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        responseBody.Should().NotBeNullOrEmpty().And.Contain("\"status\": \"Healthy\"");
        responseBody.Should().NotContain("\"status\": \"Degraded\"");
        responseBody.Should().NotContain("\"status\": \"Unhealthy\"");

        VerifyDataBridgeApiClientEndpointCalled(DataBridgeApiHealthEndpoint, Times.Once());
    }

    private void VerifyDataBridgeApiClientEndpointCalled(string requestUrl, Times times)
    {
        _appTestFixture.DataBridgeApiClientHttpMessageHandlerMock.VerifyRequest(HttpMethod.Get, requestUrl, times);
    }

    private void SetupDataBridgeApiHealthCheckRequest(string uri, HttpStatusCode httpStatusCode)
    {
        _appTestFixture.DataBridgeApiClientHttpMessageHandlerMock.SetupRequest(HttpMethod.Get, uri)
            .ReturnsResponse(httpStatusCode);
    }
}