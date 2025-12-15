using FluentAssertions;
using KeeperData.Api.Tests.Integration.Helpers;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using System.Net;
using System.Net.Http.Json;
using System.Web;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class SitesEndpointTests(MongoDbFixture mongoDbFixture, LocalStackFixture localStackFixture, ApiContainerFixture apiContainerFixture) : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly LocalStackFixture _localStackFixture = localStackFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    private const string SiteAId = "cdd668a1-40f1-47dd-9b88-54d9bdec8e4d";
    private const string SiteBId = "7e39414b-6d83-48d4-add2-7c8f2213d35a";
    private const string SiteBIdentifier1 = "45910661-198a-4653-aa8d-e98778267fce";
    private const string SiteBIdentifier2 = "eddab507-c4c4-466a-a319-c2d099614a4b";
    private const string SiteCId = "bd750b7c-672c-4880-bf5f-620ddc1dcad2";
    private const string SiteCIdentifier1 = "ecb0f3d9-7692-406f-b967-fdacf86692af";
    private List<SiteDocument> GivenTheseSites
    {
        get
        {
            var sites = new List<SiteDocument>
        {
            new SiteDocument
            {
                Id = SiteAId,
                Type = new PremisesTypeSummaryDocument { IdentifierId = "t1", Code = "Business", Description = "Business Premise" },
                State = "Active",
                LastUpdatedDate = new DateTime(2010,01,01)
            },
            new SiteDocument
            {
                Id = SiteBId,
                Type = new PremisesTypeSummaryDocument { IdentifierId = "t2", Code = "Other", Description = "Other Premise" },
                State = "Active",
                LastUpdatedDate = new DateTime(2011,01,01)
            },
            new SiteDocument
            {
                Id = SiteCId,
                Type = new PremisesTypeSummaryDocument { IdentifierId = "t1", Code = "Business", Description = "Business Premise" },
                State = "Active",
                LastUpdatedDate = new DateTime(2012,01,01)
            },
        };
            sites[1].Identifiers.AddRange(new[] {
                    new SiteIdentifierDocument {IdentifierId = "d41773fd-e9cd-453d-bdd5-ed698686c2cd", Identifier = SiteBIdentifier1 },
                    new SiteIdentifierDocument {IdentifierId = "24082208-a389-463c-ace6-075fdf357458", Identifier = SiteBIdentifier2 }});

            sites[2].Identifiers.AddRange(new[] {
                    new SiteIdentifierDocument {IdentifierId = "ac87a68c-cb01-4a87-a3d4-2947da32b63e", Identifier = SiteCIdentifier1 }});
            return sites;
        }
    }

    [Theory]
    [InlineData("WithoutParamsShouldReturnAll", null, null, null, 3, "")]
    [InlineData("WhenSearchingByType", "Business", null, null, 2, SiteAId + "," + SiteCId)]
    [InlineData("WhenSearchingByIdentifier", null, SiteBIdentifier1, null, 1, SiteBId)]
    [InlineData("WhenSearchingByIdentifier", null, SiteBIdentifier2, null, 1, SiteBId)]
    [InlineData("WhenSearchingForRecordsThatDoNotExist", null, "00000000-0000-0000-0000-000000000000", null, 0, "")]
    [InlineData("WhenSearchingByDate", null, null, "2011-01-01", 2, SiteBId + "," + SiteCId)]
    [InlineData("WhenSearchingByDateAndType", "Other", null, "2011-01-01", 1, SiteBId)]
    public async Task GivenASearchRequest_ShouldHaveExpectedResults(string scenario, string? type, string? identifier, string? dateStr, int expectedCount, string expectedIdCsv)
    {
        Console.WriteLine(scenario);
        var date = !string.IsNullOrEmpty(dateStr) ? (DateTime?)DateTime.Parse(dateStr) : null;
        var response = await _apiContainerFixture.HttpClient.GetAsync("api/site?" + BuildQueryString(type, identifier, date));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<SiteDocument>>();

        result.Should().NotBeNull();
        result.Count.Should().Be(expectedCount);

        var expectedIds = expectedIdCsv.Split(',').Where(s => !String.IsNullOrEmpty(s));
        foreach (var id in expectedIds)
            result.Values.Should().Contain(x => x.Id == id);
    }

    [Theory]
    [InlineData("WhenSearchingById1", SiteAId, HttpStatusCode.OK, SiteAId)]
    [InlineData("WhenSearchingById2", SiteBId, HttpStatusCode.OK, "\"code\":\"Other\"")]
    [InlineData("WhenSearchingForIdThatDoesNotExist", "00000000-0000-0000-0000-000000000000", HttpStatusCode.NotFound, "not found")]
    public async Task GivenAnRecordRequestById_ShouldHaveExpectedResults(string scenario, string requestedId, HttpStatusCode expectedHttpCode, string responseShouldContain)
    {
        Console.WriteLine(scenario);
        var response = await _apiContainerFixture.HttpClient.GetAsync($"api/site/{requestedId}");
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expectedHttpCode);

        if (expectedHttpCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<SiteDocument>();
            result.Should().NotBeNull();
        }

        responseBody.Should().Contain(responseShouldContain);
    }

    private static string BuildQueryString(string? type, string? identifier, DateTime? lastUpdatedDate)
    {
        var parameters = new[] {
            type != null ? $"type={HttpUtility.UrlEncode(type)}" : null,
            identifier != null ? $"siteIdentifier={HttpUtility.UrlEncode(identifier)}" : null,
            lastUpdatedDate != null ? $"lastUpdatedDate={HttpUtility.UrlEncode(lastUpdatedDate.ToString())}" : null
            };
        return String.Join('&', parameters.Where(p => p != null).ToList());
    }

    public async Task InitializeAsync()
    {
        await _mongoDbFixture.MongoVerifier.DeleteAll<SiteDocument>();
        await _mongoDbFixture.MongoVerifier.Insert(GivenTheseSites);
    }

    public async Task DisposeAsync()
    {
        await _mongoDbFixture.MongoVerifier.Delete(GivenTheseSites);
    }
}