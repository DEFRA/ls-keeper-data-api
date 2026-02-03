using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;
using System.Net;
using System.Net.Http.Json;
using System.Web;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class SitesEndpointTests(
    MongoDbFixture mongoDbFixture,
    ApiContainerFixture apiContainerFixture) : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    private const string SiteAId = "cdd668a1-40f1-47dd-9b88-54d9bdec8e4d";
    private const string SiteBId = "7e39414b-6d83-48d4-add2-7c8f2213d35a";
    private const string SiteBIdentifier1 = "45910661-198a-4653-aa8d-e98778267fce";
    private const string SiteBIdentifier2 = "eddab507-c4c4-466a-a319-c2d099614a4b";
    private const string SiteCId = "bd750b7c-672c-4880-bf5f-620ddc1dcad2";
    private const string SiteCIdentifier1 = "ecb0f3d9-7692-406f-b967-fdacf86692af";

    private static List<SiteDocument> GivenTheseSites
    {
        get
        {
            var sites = new List<SiteDocument>
            {
                new()
                {
                    Id = SiteAId,
                    Type = new PremisesTypeSummaryDocument { IdentifierId = "t1", Code = "Business", Description = "Business Premise" },
                    State = HoldingStatusType.Active.GetDescription(),
                    Name = "Site A",
                    CreatedDate = new DateTime(2010,01,01),
                    LastUpdatedDate = new DateTime(2010,01,01)
                },
                new()
                {
                    Id = SiteBId,
                    Type = new PremisesTypeSummaryDocument { IdentifierId = "t2", Code = "Other", Description = "Other Premise" },
                    State = HoldingStatusType.Active.GetDescription(),
                    Name = "Site B",
                    LastUpdatedDate = new DateTime(2011,01,01)
                },
                new()
                {
                    Id = SiteCId,
                    Type = new PremisesTypeSummaryDocument { IdentifierId = "t1", Code = "Business", Description = "Business Premise" },
                    State = HoldingStatusType.Active.GetDescription(),
                    Name = "Site C",
                    LastUpdatedDate = new DateTime(2012,01,01)
                }
            };

            sites[1].Identifiers.AddRange([
                new SiteIdentifierDocument
                {
                    IdentifierId = "d41773fd-e9cd-453d-bdd5-ed698686c2cd",
                    Identifier = SiteBIdentifier1,
                    Type = new SiteIdentifierSummaryDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Code = HoldingIdentifierType.CPHN.ToString(),
                        Description = HoldingIdentifierType.CPHN.GetDescription()!
                    }
                },
                new SiteIdentifierDocument
                {
                    IdentifierId = "24082208-a389-463c-ace6-075fdf357458",
                    Identifier = SiteBIdentifier2,
                    Type = new SiteIdentifierSummaryDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Code = HoldingIdentifierType.CPHN.ToString(),
                        Description = HoldingIdentifierType.CPHN.GetDescription()!
                    }
                }
            ]);

            sites[2].Identifiers.AddRange([
                new SiteIdentifierDocument
                {
                    IdentifierId = "ac87a68c-cb01-4a87-a3d4-2947da32b63e",
                    Identifier = SiteCIdentifier1,
                    Type = new SiteIdentifierSummaryDocument
                    {
                        IdentifierId = Guid.NewGuid().ToString(),
                        Code = HoldingIdentifierType.CPHN.ToString(),
                        Description = HoldingIdentifierType.CPHN.GetDescription()!
                    }
                }
            ]);

            return sites;
        }
    }

    [Theory]
    [InlineData("WithoutParamsShouldReturnAll", null, null, null, null, 3, "")]
    [InlineData("WhenSearchingByType", "Business", null, null, null, 2, SiteAId + "," + SiteCId)]
    [InlineData("WhenSearchingByIdentifier", null, SiteBIdentifier1, null, null, 1, SiteBId)]
    [InlineData("WhenSearchingByIdentifier", null, SiteBIdentifier2, null, null, 1, SiteBId)]
    [InlineData("WhenSearchingByMultipleIdentifiers", null, null, SiteBIdentifier1 + "," + SiteCIdentifier1, null, 2, SiteBId + "," + SiteCId)]
    [InlineData("WhenSearchingForRecordsThatDoNotExist", null, "00000000-0000-0000-0000-000000000000", null, null, 0, "")]
    [InlineData("WhenSearchingByDate", null, null, null, "2011-01-01", 2, SiteBId + "," + SiteCId)]
    [InlineData("WhenSearchingByDateAndType", "Other", null, null, "2011-01-01", 1, SiteBId)]
    public async Task GivenASearchRequest_ShouldHaveExpectedResults(string scenario, string? type, string? identifier, string? identifiers, string? dateStr, int expectedCount, string expectedIdCsv)
    {
        Console.WriteLine(scenario);
        var date = !string.IsNullOrEmpty(dateStr) ? (DateTime?)DateTime.Parse(dateStr) : null;
        var response = await _apiContainerFixture.HttpClient.GetAsync("api/sites?" + BuildQueryString(type, identifier, identifiers, date));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<SiteDocument>>();

        result.Should().NotBeNull();
        result.Count.Should().Be(expectedCount);

        var expectedIds = expectedIdCsv.Split(',').Where(s => !string.IsNullOrEmpty(s));
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
        var response = await _apiContainerFixture.HttpClient.GetAsync($"api/sites/{requestedId}");
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expectedHttpCode);

        if (expectedHttpCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<SiteDocument>();
            result.Should().NotBeNull();
        }

        responseBody.Should().Contain(responseShouldContain);
    }

    private static string BuildQueryString(string? type, string? identifier, string? identifiers, DateTime? lastUpdatedDate)
    {
        var parameters = new[] {
            type != null ? $"type={HttpUtility.UrlEncode(type)}" : null,
            identifier != null ? $"siteIdentifier={HttpUtility.UrlEncode(identifier)}" : null,
            identifiers != null ? $"siteIdentifiers={HttpUtility.UrlEncode(identifiers)}" : null,
            lastUpdatedDate != null ? $"lastUpdatedDate={HttpUtility.UrlEncode(lastUpdatedDate.ToString())}" : null
        };

        return string.Join('&', parameters.Where(p => p != null).ToList());
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