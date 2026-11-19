using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;

using KeeperData.Core.Extensions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class SitesEndpointTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    [Fact]
    public async Task GetSites_WithFilterAndSort_ReturnsFilteredAndSortedOkResult()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var keeperPartyId = Guid.NewGuid();
        var sites = new List<SiteDocument> { CreateSite("Site A", "Type1", "ID1") };

        SetupRepository(sites, totalCount: 1);

        var query = $"?siteIdentifier=ID1&type=Type1&siteId={siteId}&keeperPartyId={keeperPartyId}&order=name&sort=asc";

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/sites{query}");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 1, expectedNames: ["Site A"]);
    }

    [Fact]
    public async Task GetSites_WithoutParameters_ReturnsDefaultOkResult()
    {
        // Arrange
        var sites = new List<SiteDocument>
        {
            CreateSite("Site B", "Type2", "ID2"),
            CreateSite("Site A", "Type1", "ID1")
        };

        SetupRepository(sites, totalCount: 2);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/sites");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Site B", "Site A"]);
    }

    [Fact]
    public async Task GetSites_WithCommaSeparatedTypes_ReturnsFilteredResult()
    {
        // Arrange
        var sites = new List<SiteDocument>
        {
            CreateSite("Site A", "Type1", "ID1"),
            CreateSite("Site B", "Type2", "ID2")
        };

        SetupRepository(sites, totalCount: 2);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/sites?type=Type1,Type2");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Site A", "Site B"]);
    }

    [Fact]
    public async Task GetSites_WithCommaSeparatedAndMessySiteIdentifiers_ParsesAndReturnsFilteredResult()
    {
        // Arrange
        var sites = new List<SiteDocument>
        {
            CreateSite("Site A", "Type1", "ID1"),
            CreateSite("Site B", "Type1", "ID2")
        };

        SetupRepository(sites, totalCount: 2);

        // Input contains spaces and an empty entry to force the parsing logic to work
        var messyIdentifiers = " ID1 , , ID2 ";
        var encodedIdentifiers = HttpUtility.UrlEncode(messyIdentifiers);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/sites?siteIdentifiers={encodedIdentifiers}");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Site A", "Site B"]);
    }

    [Fact]
    public async Task GetSiteById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var siteId = Guid.NewGuid().ToString();
        var site = CreateSite("Site By ID", "Type1", "ID1");
        site.Id = siteId;

        _appTestFixture.AppWebApplicationFactory._goldSiteRepositoryMock
            .Setup(r => r.GetByIdAsync(siteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/sites/{siteId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SiteDocument>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(siteId);
        result.Name.Should().Be("Site By ID");
    }

    [Fact]
    public async Task GetSites_WithCommaSeparatedSiteIds_AppliesAllIdsAndReturnsFilteredResult()
    {
        // Arrange
        var site1Id = Guid.NewGuid();
        var site2Id = Guid.NewGuid();

        var sites = new List<SiteDocument>
        {
            CreateSite("Site 1", "Type1", "ID1"),
            CreateSite("Site 2", "Type1", "ID2")
        };
        sites[0].Id = site1Id.ToString();
        sites[1].Id = site2Id.ToString();

        FilterDefinition<SiteDocument>? capturedFilter = null;
        SetupRepository(sites, totalCount: 2, onFind: f => capturedFilter = f);

        // Surrounding whitespace and empty entries between commas are ignored
        var idsParam = $" {site1Id} , , {site2Id} ";
        var encodedIds = HttpUtility.UrlEncode(idsParam);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/sites?siteIds={encodedIds}");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Site 1", "Site 2"]);

        capturedFilter.Should().NotBeNull();
        var rendered = capturedFilter!.Render(new RenderArgs<SiteDocument>(
            BsonSerializer.SerializerRegistry.GetSerializer<SiteDocument>(), BsonSerializer.SerializerRegistry));
        rendered.ToString().Should().Contain(site1Id.ToString()).And.Contain(site2Id.ToString());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not-a-guid,also-not-a-guid")]
    [InlineData("{0},invalid")]
    [InlineData("invalid, {0}")]
    [InlineData(",")]
    [InlineData(" , , ")]
    [InlineData("   ")]
    [InlineData("")]
    public async Task GetSites_WithMalformedSiteIds_ReturnsBadRequestWithoutQueryingRepository(string siteIdsTemplate)
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        var siteIds = string.Format(siteIdsTemplate, Guid.NewGuid());

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/sites?siteIds={HttpUtility.UrlEncode(siteIds)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        body.RootElement.GetProperty("errors").TryGetProperty("SiteIds", out _).Should().BeTrue();

        VerifyNoRepositoryQueries();
    }

    [Fact]
    public async Task GetSites_WithMoreSiteIdsThanAllowed_ReturnsExistingLimitValidationResponse()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        var siteIds = string.Join(",", Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()));

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/sites?siteIds={HttpUtility.UrlEncode(siteIds)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("SiteIds", out _).Should().BeTrue();

        VerifyNoRepositoryQueries();
    }

    private static SiteDocument CreateSite(string name, string typeCode, string identifier)
    {
        var site = new SiteDocument
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Type = new SiteTypeSummaryDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Code = typeCode,
                Name = $"{typeCode} Description"
            },
            State = "active"
        };

        site.Identifiers.Add(new SiteIdentifierDocument
        {
            IdentifierId = $"test-id-{identifier}",
            Identifier = identifier,
            Type = new SiteIdentifierSummaryDocument()
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Code = "CPHN",
                Name = "CPH Number"
            },
            LastUpdatedDate = DateTime.UtcNow
        });

        return site;
    }

    private void SetupRepository(List<SiteDocument> sites, int totalCount, Action<FilterDefinition<SiteDocument>>? onFind = null)
    {
        _appTestFixture.AppWebApplicationFactory._sitesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(),
                0, 10, It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<SiteDocument>, SortDefinition<SiteDocument>, int, int, CancellationToken>(
                (filter, _, _, _, _) => onFind?.Invoke(filter))
            .ReturnsAsync(sites);

        _appTestFixture.AppWebApplicationFactory._sitesRepositoryMock
            .Setup(r => r.CountAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);
    }

    private static async Task AssertPaginatedResponse(HttpResponseMessage response, int expectedCount, IEnumerable<string> expectedNames)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<SiteDocument>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(expectedCount);
        result.Values.Should().HaveCount(expectedCount);

        result.Values.Select(v => v.Name).Should().BeEquivalentTo(expectedNames);
    }

    private void VerifyNoRepositoryQueries()
    {
        var sitesRepositoryMock = _appTestFixture.AppWebApplicationFactory._sitesRepositoryMock;

        sitesRepositoryMock.Verify(r => r.CountAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        sitesRepositoryMock.Verify(r => r.FindAsync(
            It.IsAny<FilterDefinition<SiteDocument>>(),
            It.IsAny<SortDefinition<SiteDocument>>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}