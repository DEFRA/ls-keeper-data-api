using Amazon.Runtime;
using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;
using System.Net;
using System.Net.Http.Json;

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