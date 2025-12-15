using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class HealthcheckEndpointTests(
    MongoDbFixture mongoDbFixture,
    LocalStackFixture localStackFixture,
    ApiContainerFixture apiContainerFixture)
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    [Fact]
    public async Task GivenValidHealthCheckRequest_ShouldSucceed()
    {
        var response = await _apiContainerFixture.HttpClient.GetAsync("health");
        var responseBody = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        responseBody.Should().NotBeNullOrEmpty().And.Contain("\"status\": \"Healthy\"");
        responseBody.Should().NotContain("\"status\": \"Degraded\"");
        responseBody.Should().NotContain("\"status\": \"Unhealthy\"");
    }
}