using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Json;
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

    private static SiteDocument CreateSite(string name, string typeCode, string identifier)
    {
        var site = new SiteDocument
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Type = new PremisesTypeSummaryDocument
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Code = typeCode,
                Description = $"{typeCode} Description"
            },
            State = HoldingStatusType.Active.GetDescription()
        };

        site.Identifiers.Add(new SiteIdentifierDocument
        {
            IdentifierId = $"test-id-{identifier}",
            Identifier = identifier,
            Type = new SiteIdentifierSummaryDocument()
            {
                IdentifierId = Guid.NewGuid().ToString(),
                Code = HoldingIdentifierType.CPHN.ToString(),
                Description = HoldingIdentifierType.CPHN.GetDescription()!
            },
            LastUpdatedDate = DateTime.UtcNow
        });

        return site;
    }

    private void SetupRepository(List<SiteDocument> sites, int totalCount)
    {
        _appTestFixture.AppWebApplicationFactory._sitesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(),
                0, 10, It.IsAny<CancellationToken>()))
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
}