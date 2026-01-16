using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using System.Net;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class DbDropCollectionEndpointTests(ApiContainerFixture fixture)
{
    private readonly ApiContainerFixture _fixture = fixture;

    [Fact]
    public async Task WipeShouldReturnOK()
    {
        var response = await _fixture.HttpClient.PostAsync("api/dbdropcollection/parties", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}