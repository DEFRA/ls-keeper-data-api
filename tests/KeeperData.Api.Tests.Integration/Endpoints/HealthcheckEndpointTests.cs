using Amazon.S3.Model;
using Amazon.S3;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Helpers;
using MongoDB.Driver;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class HealthcheckEndpointTests
{
    private readonly MongoDbFixture _mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture;

    public HealthcheckEndpointTests(MongoDbFixture mongoDbFixture, LocalStackFixture localStackFixture, ApiContainerFixture apiContainerFixture)
    {
        _mongoDbFixture = mongoDbFixture;
        _localStackFixture = localStackFixture;
        _apiContainerFixture = apiContainerFixture;
    }

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