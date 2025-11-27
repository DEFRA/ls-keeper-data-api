using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using System.Net;
using System.Net.Http.Json;
using System.Web;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Trait("Dependence", "localstack")]
[Collection("Integration Tests")]
public class PartiesEndpointTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly HttpClient _httpClient = fixture.HttpClient;
    private readonly IntegrationTestFixture _fixture = fixture;

    private const string JohnSmithId = "2b156a83-3b8d-4393-96ca-94d2df7eea27";
    private const string MarkSmithId = "ceef6fbc-cc67-4272-9c40-00ae257e62e0";
    private const string HueyNewsId = "d0f35ad0-0a41-4ea4-84e0-10d56344b1c4";
    private readonly List<PartyDocument> GivenTheseParties = new List<PartyDocument>
        {
            new PartyDocument
            {
                Id = JohnSmithId,
                FirstName = "John",
                LastName = "Smith",
                State = "Active",
                LastUpdatedDate = new DateTime(2010,01,01)
            },
            new PartyDocument
            {
                Id = MarkSmithId,
                FirstName = "Mark",
                LastName = "Smith",
                State = "Active",
                LastUpdatedDate = new DateTime(2011,01,01)
            },
            new PartyDocument
            {
                Id = HueyNewsId,
                FirstName = "Huey",
                LastName = "News",
                State = "Active",
                LastUpdatedDate = new DateTime(2012,01,01)
            },
        };

    [Theory]
    [InlineData("WithoutParamsShouldReturnAll", null, null, null, 3, "")]
    [InlineData("WhenSearchingByFirstName", "John", null, null, 1, JohnSmithId)]
    [InlineData("WhenSearchingByLastName", null, "Smith", null, 2, JohnSmithId + "," + MarkSmithId)]
    [InlineData("WhenSearchingByFirstAndLastName", "Mark", "Smith", null, 1, MarkSmithId)]
    [InlineData("WhenSearchingForRecordsThatDoNotExist", null, "Smythe", null, 0, "")]
    [InlineData("WhenSearchingByDate", null, null, "2011-01-01", 2, MarkSmithId + "," + HueyNewsId)]
    // TODO case insensitive search [InlineData("WhenSearchingCaseInsensitive", "john", "smith", 1, JohnSmithId)]
    public async Task GivenASearchRequest_ShouldHaveExpectedResults(string scenario, string? firstName, string? lastName, string? dateStr, int expectedCount, string expectedIdCsv)
    {
        Console.WriteLine(scenario);
        var date = !string.IsNullOrEmpty(dateStr) ? (DateTime?)DateTime.Parse(dateStr) : null;
        var response = await _httpClient.GetAsync("api/party?" + BuildQueryString(firstName, lastName, date));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<PartyDocument>>();

        result.Should().NotBeNull();
        result.Count.Should().Be(expectedCount);

        var expectedIds = expectedIdCsv.Split(',').Where(s => !String.IsNullOrEmpty(s));
        foreach (var id in expectedIds)
            result.Values.Should().Contain(x => x.Id == id);
    }

    [Theory]
    [InlineData("WhenSearchingById1", JohnSmithId, HttpStatusCode.OK, "\"firstName\":\"John\"")]
    [InlineData("WhenSearchingById2", HueyNewsId, HttpStatusCode.OK, "\"firstName\":\"Huey\"")]
    [InlineData("WhenSearchingForIdThatDoesNotExist", "00000000-0000-0000-0000-000000000000", HttpStatusCode.InternalServerError, "not found")] //TODO should this be 404
    public async Task GivenAnRecordRequestById_ShouldHaveExpectedResults(string scenario, string requestedId, HttpStatusCode expectedHttpCode, string responseShouldContain)
    {
        Console.WriteLine(scenario);
        var response = await _httpClient.GetAsync($"api/party/{requestedId}");
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expectedHttpCode);

        if (expectedHttpCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<PartyDocument>();
            result.Should().NotBeNull();
        }

        responseBody.Should().Contain(responseShouldContain);
    }

    private static string BuildQueryString(string? firstName, string? lastName, DateTime? lastUpdatedDate)
    {
        var parameters = new[] {
            firstName != null ? $"firstName={HttpUtility.UrlEncode(firstName)}" : null,
            lastName != null ? $"lastName={HttpUtility.UrlEncode(lastName)}" : null,
            lastUpdatedDate != null ? $"lastUpdatedDate={HttpUtility.UrlEncode(lastUpdatedDate.ToString())}" : null
            };
        return String.Join('&', parameters.Where(p => p != null).ToList());
    }

    public async Task InitializeAsync()
    {
        await _fixture.MongoVerifier.DeleteAll<PartyDocument>();
        await _fixture.MongoVerifier.Insert(GivenTheseParties);
    }

    public async Task DisposeAsync()
    {
        await _fixture.MongoVerifier.Delete(GivenTheseParties);
    }
}