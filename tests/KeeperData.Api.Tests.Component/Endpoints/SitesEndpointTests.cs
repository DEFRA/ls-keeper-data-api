using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Json;

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

        var site = new SiteDocument
        {
            Id = siteId.ToString(),
            Name = "Site A",
            Type = "Type1",
            State = "Active"
        };
        site.Identifiers.Add(new SiteIdentifierDocument { IdentifierId = "test-id-1", Identifier = "ID1", Type = "CPH", LastUpdatedDate = DateTime.UtcNow });
        var sites = new List<SiteDocument> { site };


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
        var siteA = new SiteDocument
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Site A",
            Type = "Type1",
            State = "Active"
        };
        siteA.Identifiers.Add(new SiteIdentifierDocument { IdentifierId = "test-id-1", Identifier = "ID1", Type = "CPH", LastUpdatedDate = DateTime.UtcNow });

        var siteB = new SiteDocument
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Site B",
            Type = "Type2",
            State = "Inactive"
        };
        siteB.Identifiers.Add(new SiteIdentifierDocument { IdentifierId = "test-id-2", Identifier = "ID2", Type = "CPH", LastUpdatedDate = DateTime.UtcNow });

        var sites = new List<SiteDocument> { siteB, siteA };

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