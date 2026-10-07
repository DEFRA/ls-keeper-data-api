using FluentAssertions;
using KeeperData.Tests.Common.Utilities;
using Moq;
using System.Net;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class LegacyEndpointsTests
{
    private const string BasicApiKey = "ApiKey";
    private const string BasicSecret = "integration-test-secret";

    [Theory]
    [InlineData("/api/countries")]
    [InlineData("/api/parties")]
    [InlineData("/api/sites")]
    [InlineData("/api/sitetypes")]
    [InlineData("/api/species")]
    [InlineData("/api/reference/activities")]
    [InlineData("/api/reference/productionusages")]
    [InlineData("/api/reference/roles")]
    [InlineData("/api/reference/sitetypes")]
    [InlineData("/api/reference/IdentifierTypes")]
    public async Task Get_WhenLegacyEndpointsDisabled_ReturnsNotFound(string route)
    {
        using var factory = CreateFactory(legacyEndpointsEnabled: false);
        using var client = factory.CreateClient();
        client.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/countries")]
    [InlineData("/api/parties")]
    public async Task Get_WhenLegacyEndpointsEnabled_RoutesToController(string route)
    {
        using var factory = CreateFactory(legacyEndpointsEnabled: true);
        factory._countryRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        using var client = factory.CreateClient();
        client.AddBasicApiKey(BasicApiKey, BasicSecret);

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WhenLegacyEndpointsDisabled_V2RoutesRemainAvailable()
    {
        using var factory = CreateFactory(legacyEndpointsEnabled: false);
        using var client = factory.CreateClient();
        client.AddBasicApiKey(BasicApiKey, BasicSecret);

        // The cache-backed v2 controller returns 503 when its SQLite cache has not loaded,
        // which proves the request was routed rather than rejected by the legacy gate.
        var response = await client.GetAsync("/api/v2/cphs");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Get_WhenLegacyEndpointsDisabled_HealthEndpointIsNotBlocked()
    {
        using var factory = CreateFactory(legacyEndpointsEnabled: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(TestConstants.HealthCheckEndpoint);

        // The health endpoint is outside the legacy gate; it may report 200 or 503
        // depending on the mocked dependencies, but it must never be rejected as a
        // legacy route.
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    private static AppWebApplicationFactory CreateFactory(bool legacyEndpointsEnabled)
    {
        return new AppWebApplicationFactory(new Dictionary<string, string?>
        {
            ["LegacyEndpointsEnabled"] = legacyEndpointsEnabled.ToString().ToLowerInvariant()
        });
    }
}