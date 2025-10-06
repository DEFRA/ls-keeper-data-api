using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using KeeperData.Application.Queries.Pagination;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class SitesEndpointTests : IClassFixture<AppTestFixture>
{
    private readonly AppWebApplicationFactory _factory;
    private readonly Mock<ISitesRepository> _sitesRepositoryMock;
    private readonly HttpClient _client;

    public SitesEndpointTests(AppTestFixture fixture)
    {
        _factory = fixture.AppWebApplicationFactory;
        _sitesRepositoryMock = new Mock<ISitesRepository>();

        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISitesRepository>();
                services.AddScoped(_ => _sitesRepositoryMock.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetSites_WithFilterAndSort_ReturnsFilteredAndSortedOkResult()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var keeperPartyId = Guid.NewGuid();

        var sites = new List<SiteDocument>
        {
            new() { Id = siteId.ToString(), Name = "Site A", Type = "Type1", State = "Active", PrimaryIdentifier = "ID1", KeeperPartyIds = [keeperPartyId.ToString()] }
        };

        _sitesRepositoryMock.Setup(r => r.FindAsync(It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(), It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(), 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sites);
        _sitesRepositoryMock.Setup(r => r.CountAsync(It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = $"?siteIdentifier=ID1&type=Type1&siteId={siteId}&keeperPartyId={keeperPartyId}&order=name&sort=asc";

        // Act
        var response = await _client.GetAsync($"/api/site{query}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<SiteDocument>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(1);
        result.Values.Should().HaveCount(1);
        result.Values[0].Name.Should().Be("Site A");

        _sitesRepositoryMock.Verify(r => r.FindAsync(It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(), It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(), 0, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSites_WithoutParameters_ReturnsDefaultOkResult()
    {
        // Arrange
        var sites = new List<SiteDocument>
        {
            new() { Id = Guid.NewGuid().ToString(), Name = "Site B", Type = "Type2", State = "Inactive", PrimaryIdentifier = "ID2" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Site A", Type = "Type1", State = "Active", PrimaryIdentifier = "ID1" }
        };

        _sitesRepositoryMock.Setup(r => r.FindAsync(It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(), It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(), 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sites);
        _sitesRepositoryMock.Setup(r => r.CountAsync(It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var response = await _client.GetAsync("/api/site");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<SiteDocument>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
        result.Values.Should().HaveCount(2);

        _sitesRepositoryMock.Verify(r => r.FindAsync(It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(), It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(), 0, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}