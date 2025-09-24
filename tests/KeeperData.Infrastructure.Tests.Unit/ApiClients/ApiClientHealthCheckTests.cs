using FluentAssertions;
using KeeperData.Infrastructure.ApiClients.Setup;
using KeeperData.Infrastructure.Tests.Unit.ApiClients.Helpers;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using System.Net;

namespace KeeperData.Infrastructure.Tests.Unit.ApiClients;

public class ApiClientHealthCheckTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly HealthCheckContext _context = new();
    private const string ClientName = "TestClient";
    private const string Endpoint = "/health";

    [Fact]
    public async Task CheckHealthAsync_WhenResponseIsSuccess_ReturnsHealthy()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var sut = CreateSut(response);

        var result = await sut.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResponseIsFailure_ReturnsDegraded()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            ReasonPhrase = "Internal server error"
        };
        var sut = CreateSut(response);

        var result = await sut.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Exception.Should().BeNull();

        result.Data["status-code"].Should().Be(HttpStatusCode.InternalServerError);
        result.Data["reason"].Should().Be("Internal server error");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRequestTimesOut_ReturnsUnhealthyWithTimeoutException()
    {
        var sut = new ApiClientHealthCheck(_httpClientFactoryMock.Object, ClientName, Endpoint, timeoutSeconds: 1);

        var client = new HttpClient(new TimeoutHandler()) { BaseAddress = new Uri("http://localhost") };
        _httpClientFactoryMock.Setup(f => f.CreateClient(ClientName)).Returns(client);

        var result = await sut.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<TimeoutException>();
        result.Exception.Message.Should().Contain("timed out");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRequestThrowsException_ReturnsUnhealthyWithException()
    {
        var sut = CreateSutWithException(new InvalidOperationException("Invalid operation"));

        var result = await sut.CheckHealthAsync(_context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Exception.Message.Should().Contain("Invalid operation");
    }

    private class TimeoutHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken); // Simulate delay
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private ApiClientHealthCheck CreateSut(HttpResponseMessage response)
    {
        var testMessageHandler = new TestHttpMessageHandler((req, token) => Task.FromResult(response));

        var client = new HttpClient(testMessageHandler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        _httpClientFactoryMock.Setup(f => f.CreateClient(ClientName)).Returns(client);

        return new ApiClientHealthCheck(_httpClientFactoryMock.Object, ClientName, Endpoint);
    }

    private ApiClientHealthCheck CreateSutWithException(Exception ex)
    {
        var testMessageHandler = new TestHttpMessageHandler((req, token) =>
        {
            throw ex;
        });

        var client = new HttpClient(testMessageHandler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        };

        _httpClientFactoryMock.Setup(f => f.CreateClient(ClientName)).Returns(client);

        return new ApiClientHealthCheck(_httpClientFactoryMock.Object, ClientName, Endpoint);
    }
}