using FluentAssertions;
using KeeperData.Api.Controllers.Admin;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Utilities;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class AdminCacheEndpointTests
{
    private const string Route = "/api/admin/sqlite-cache/refresh";

    [Fact]
    public async Task RefreshAll_ReturnsBothResults()
    {
        using var factory = CreateFactory(true, out var cph, out var readModel);
        using var client = factory.CreateClient();
        client.AddBasicApiKey("ApiKey", "integration-test-secret");

        using var response = await client.PostAsync(Route, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CacheRefreshResponseForTest>();
        result!.Results.Select(x => x.Cache).Should().Equal("CPH", "Read model");
        cph.Verify(x => x.ForceRefreshAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        readModel.Verify(x => x.ForceRefreshAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshReadModel_OnlyRefreshesReadModel()
    {
        using var factory = CreateFactory(true, out var cph, out var readModel);
        using var client = factory.CreateClient();
        client.AddBasicApiKey("ApiKey", "integration-test-secret");

        using var response = await client.PostAsync($"{Route}?cache=read-model&force=false", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        cph.Verify(x => x.ForceRefreshAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        readModel.Verify(x => x.ForceRefreshAsync(false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidCache_ReturnsBadRequest()
    {
        using var factory = CreateFactory(true, out _, out _);
        using var client = factory.CreateClient();
        client.AddBasicApiKey("ApiKey", "integration-test-secret");

        using var response = await client.PostAsync($"{Route}?cache=unknown", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingCredentials_ReturnsUnauthorized()
    {
        using var factory = CreateFactory(true, out _, out _);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(Route, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BearerWithoutAdminScope_ReturnsForbidden()
    {
        using var factory = CreateFactory(true, out _, out _, useFakeAuth: true);
        using var client = factory.CreateClient();
        client.AddJwt();

        using var response = await client.PostAsync(Route, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Disabled_ReturnsNotFoundEvenWithoutCredentials()
    {
        using var factory = CreateFactory(false, out _, out _);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(Route, null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Disabled_WhenControllerIsInvokedDirectly_ReturnsNotFound()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminEndpointsEnabled"] = "false"
            })
            .Build();
        var controller = new AdminCacheController(
            Mock.Of<ICphSqliteCacheService>(),
            Mock.Of<IReadModelSqliteCacheService>(),
            configuration);

        var result = await controller.Refresh();

        result.Should().BeOfType<Microsoft.AspNetCore.Mvc.NotFoundResult>();
    }

    [Fact]
    public async Task BridgeFailure_ReturnsBadGateway()
    {
        using var factory = CreateFactory(true, out var cph, out _);
        cph.Setup(x => x.ForceRefreshAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CacheRefreshResult("CPH", "Failed", null, null, null, 3, "Bridge unavailable"));
        using var client = factory.CreateClient();
        client.AddBasicApiKey("ApiKey", "integration-test-secret");

        using var response = await client.PostAsync($"{Route}?cache=cph", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Bridge unavailable");
    }

    [Fact]
    public async Task InternalSwagger_IncludesRefreshEndpoint()
    {
        using var factory = CreateFactory(true, out _, out _);
        using var client = factory.CreateClient();

        var swagger = await client.GetStringAsync("/swagger/internal/swagger.json");

        swagger.Should().Contain(Route);
    }

    private static AppWebApplicationFactory CreateFactory(
        bool enabled,
        out Mock<ICphSqliteCacheService> cph,
        out Mock<IReadModelSqliteCacheService> readModel,
        bool useFakeAuth = false)
    {
        cph = new Mock<ICphSqliteCacheService>();
        readModel = new Mock<IReadModelSqliteCacheService>();
        cph.Setup(x => x.ForceRefreshAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CacheRefreshResult("CPH", "Reloaded", "cphs.sqlite", 2, DateTime.UtcNow, 1));
        readModel.Setup(x => x.ForceRefreshAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CacheRefreshResult("Read model", "Reloaded", "krds-db.sqlite", 3, DateTime.UtcNow, 1));

        var factory = new AppWebApplicationFactory(new Dictionary<string, string?>
        {
            ["AdminEndpointsEnabled"] = enabled.ToString().ToLowerInvariant()
        }, useFakeAuth);
        factory.OverrideServiceAsSingleton(cph.Object);
        factory.OverrideServiceAsSingleton(readModel.Object);
        return factory;
    }

    private sealed record CacheRefreshResponseForTest(DateTime Timestamp, CacheRefreshResult[] Results);
}