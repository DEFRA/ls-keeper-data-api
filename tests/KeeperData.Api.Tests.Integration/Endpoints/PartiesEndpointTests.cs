using System.Net;
using System.Net.Http.Json;
using System.Web;
using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Trait("Dependence", "localstack")]
[Collection("Database collection")]
public class PartiesEndpointTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{    
    private readonly HttpClient _httpClient = fixture.HttpClient;
    private readonly IntegrationTestFixture _fixture = fixture;

    private readonly List<PartyDocument> GivenTheseParties = new List<PartyDocument>
        {
            new PartyDocument
            {
                Id = "2b156a83-3b8d-4393-96ca-94d2df7eea27",
                FirstName = "John",
                LastName = "Smith",
                State = "Active"
            },
            new PartyDocument
            {
                Id = "ceef6fbc-cc67-4272-9c40-00ae257e62e0",
                FirstName = "Mark",
                LastName = "Smith",
                State = "Active"
            },
            new PartyDocument
            {
                Id = "d0f35ad0-0a41-4ea4-84e0-10d56344b1c4",
                FirstName = "Huey",
                LastName = "News",
                State = "Active"
            },
        };

    [Theory]
    [InlineData("WithoutParamsShouldReturnAll", null, null, 3, "")]
    [InlineData("WhenSearchingByFirstName", "John", null, 1, "2b156a83-3b8d-4393-96ca-94d2df7eea27")]
    [InlineData("WhenSearchingByLastName", null, "Smith", 2, "2b156a83-3b8d-4393-96ca-94d2df7eea27,ceef6fbc-cc67-4272-9c40-00ae257e62e0")]
    [InlineData("WhenSearchingByFirstAndLastName", "Mark", "Smith", 1, "ceef6fbc-cc67-4272-9c40-00ae257e62e0")]
    [InlineData("WhenSearchingForRecordsThatDoNotExist", null, "Smythe", 0, "")]
    // TODO case insensitive search [InlineData("WhenSearchingCaseInsensitive", "john", "smith", 1, "2b156a83-3b8d-4393-96ca-94d2df7eea27")]
    public async Task GivenASearchRequest_ShouldHaveExpectedResults(string scenario, string firstName, string lastName, int expectedCount, string expectedIdCsv)
    {
        var response = await _httpClient.GetAsync("api/party?" + BuildQueryString(firstName, lastName));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<PartyDocument>>();

        result.Should().NotBeNull();
        result.Count.Should().Be(expectedCount);

        var expectedIds = expectedIdCsv.Split(',').Where(s => !String.IsNullOrEmpty(s));
        foreach(var id in expectedIds)
            result.Values.Should().Contain(x => x.Id == id);
    }

    [Theory]
    [InlineData("WhenSearchingById1", "2b156a83-3b8d-4393-96ca-94d2df7eea27", HttpStatusCode.OK, "\"firstName\":\"John\"")]
    [InlineData("WhenSearchingById2", "d0f35ad0-0a41-4ea4-84e0-10d56344b1c4", HttpStatusCode.OK, "\"firstName\":\"Huey\"")]
    [InlineData("WhenSearchingForIdThatDoesNotExist", "00000000-0000-0000-0000-000000000000", HttpStatusCode.NotFound, "abc")]
    // TODO case insensitive search [InlineData("WhenSearchingCaseInsensitive", "john", "smith", 1, "2b156a83-3b8d-4393-96ca-94d2df7eea27")]
    public async Task GivenAnRecordRequestById_ShouldHaveExpectedResults(string scenario, string requestedId, HttpStatusCode expectedHttpCode, string responseShouldContain)
    {
        var response = await _httpClient.GetAsync($"api/party/{requestedId}");
        response.StatusCode.Should().Be(expectedHttpCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<PartyDocument>();

        result.Should().NotBeNull();

        responseBody.Should().Contain(responseShouldContain);
    }

    private static string BuildQueryString(string firstName, string lastName)
    {
        var parameters= new [] { 
            firstName != null ? $"firstName={HttpUtility.UrlEncode(firstName)}" : null,
            lastName != null ? $"lastName={HttpUtility.UrlEncode(lastName)}" : null
            };
        return String.Join('&', parameters.Where(p => p != null).ToList());
    }

    public async Task InitializeAsync()
    {
        await _fixture.MongoVerifier.Insert(GivenTheseParties);
    }

    public async Task DisposeAsync()
    {
        await _fixture.MongoVerifier.Delete(GivenTheseParties);
    }
}