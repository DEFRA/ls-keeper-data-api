using FluentAssertions;
using KeeperData.Core.Documents;
using System.Net;
using System.Net.Http.Json;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Trait("Dependence", "localstack")]
[Collection("Integration Tests")]
public class CountriesEndpointTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture = fixture;
    [Fact]
    public async Task WhenEndpointHitWithNoParamsAllCountriesShouldBeReturned()
    {
        var result = await WhenPerformGetOnCountriesEndpoint(HttpStatusCode.OK);

        // then all countries returned with format
        result.Count().Should().Be(249);

        Assert.Fail("wrong format / data");
    }

    private async Task<List<CountrySummaryDocument>> WhenPerformGetOnCountriesEndpoint(HttpStatusCode expectedHttpCode)
    {
        var response = await _fixture.HttpClient.GetAsync("api/countries");
        if (expectedHttpCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<List<CountrySummaryDocument>>();
            return result ?? new List<CountrySummaryDocument>();
        }
        
        return new List<CountrySummaryDocument>();
    }
}