using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using Moq;
using System.Net;
using System.Text.Json;
using System.Web;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class CursorPaginationEndpointTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;

    public static TheoryData<string, string> MalformedCursors()
    {
        var data = new TheoryData<string, string>();
        foreach (var endpoint in new[] { "/api/sites", "/api/parties" })
        {
            data.Add(endpoint, "not-base-64!!!");   // invalid Base64
            data.Add(endpoint, "U21pdGgxMjM=");     // "Smith123" - no delimiter
            data.Add(endpoint, "U21pdGh8");         // "Smith|" - no record id
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(MalformedCursors))]
    public async Task GetList_WithMalformedCursor_ReturnsBadRequestWithoutQueryingRepository(string endpoint, string cursor)
    {
        _appTestFixture.AppWebApplicationFactory.ResetMocks();

        // A valid page number must not cause a silent fallback to page-based pagination.
        var response = await _appTestFixture.HttpClient.GetAsync($"{endpoint}?page=2&cursor={HttpUtility.UrlEncode(cursor)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        body.RootElement.GetProperty("title").GetString().Should().Be("Invalid Cursor");
        body.RootElement.GetProperty("errors").TryGetProperty("Cursor", out _).Should().BeTrue();

        VerifyNoRepositoryQueries();
    }

    [Theory]
    [InlineData("/api/sites", "")]
    [InlineData("/api/sites", "%20%20")]
    [InlineData("/api/parties", "")]
    [InlineData("/api/parties", "%20%20")]
    public async Task GetList_WithBlankCursor_UsesPageBasedPagination(string endpoint, string encodedCursor)
    {
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        SetupRepositories();

        var response = await _appTestFixture.HttpClient.GetAsync($"{endpoint}?page=2&pageSize=5&cursor={encodedCursor}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        VerifyFindCalledWithSkip(endpoint, expectedSkip: 5, expectedTake: 5);
    }

    [Theory]
    [InlineData("/api/sites")]
    [InlineData("/api/parties")]
    public async Task GetList_WithValidCursor_IgnoresPageAndUsesCursor(string endpoint)
    {
        _appTestFixture.AppWebApplicationFactory.ResetMocks();
        SetupRepositories();

        var cursor = CursorHelper.Encode("Smith|Jones", Guid.NewGuid().ToString());
        var response = await _appTestFixture.HttpClient.GetAsync($"{endpoint}?page=2&pageSize=5&cursor={HttpUtility.UrlEncode(cursor)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        VerifyFindCalledWithSkip(endpoint, expectedSkip: 0, expectedTake: 5);
    }

    private void SetupRepositories()
    {
        var factory = _appTestFixture.AppWebApplicationFactory;

        factory._sitesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        factory._sitesRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        factory._partiesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        factory._partiesRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    private void VerifyFindCalledWithSkip(string endpoint, int expectedSkip, int expectedTake)
    {
        var factory = _appTestFixture.AppWebApplicationFactory;

        if (endpoint == "/api/sites")
        {
            factory._sitesRepositoryMock.Verify(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(),
                expectedSkip, expectedTake, It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            factory._partiesRepositoryMock.Verify(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                expectedSkip, expectedTake, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private void VerifyNoRepositoryQueries()
    {
        var factory = _appTestFixture.AppWebApplicationFactory;

        factory._sitesRepositoryMock.Verify(r => r.CountAsync(It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        factory._sitesRepositoryMock.Verify(r => r.FindAsync(
            It.IsAny<MongoDB.Driver.FilterDefinition<SiteDocument>>(),
            It.IsAny<MongoDB.Driver.SortDefinition<SiteDocument>>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

        factory._partiesRepositoryMock.Verify(r => r.CountAsync(It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        factory._partiesRepositoryMock.Verify(r => r.FindAsync(
            It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
            It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}