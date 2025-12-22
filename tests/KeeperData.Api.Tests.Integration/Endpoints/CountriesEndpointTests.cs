using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using System.Net;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class CountriesEndpointTests(ApiContainerFixture fixture)
{
    private readonly ApiContainerFixture _fixture = fixture;

    [Fact]
    public async Task WhenEndpointHitWithNoParamsAllCountriesShouldBeReturned()
    {
        var response = await _fixture.HttpClient.GetAsync("api/countries");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}