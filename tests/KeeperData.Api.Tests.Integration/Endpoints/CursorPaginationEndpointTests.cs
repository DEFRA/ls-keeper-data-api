using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Web;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class CursorPaginationEndpointTests(
    MongoDbFixture mongoDbFixture,
    ApiContainerFixture apiContainerFixture) : IAsyncLifetime
{
    private readonly MongoDbFixture _mongoDbFixture = mongoDbFixture;
    private readonly ApiContainerFixture _apiContainerFixture = apiContainerFixture;

    // Duplicate names force the cursor to fall back to the record id as a tie-breaker,
    // and names containing the cursor separator exercise last-separator decoding.
    private static readonly string[] Names = ["Alpha", "Bravo", "Bravo", "Bravo", "Charlie|Delta", "Charlie|Delta", "Echo"];

    private readonly List<PartyDocument> _parties = Names.Select((name, index) => new PartyDocument
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        CustomerNumber = $"CURSOR-TEST-{index}",
        State = "active",
        LastUpdatedDate = new DateTime(2020, 01, 01)
    }).ToList();

    private readonly List<SiteDocument> _sites = Names.Select(name => new SiteDocument
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        State = "active",
        LastUpdatedDate = new DateTime(2020, 01, 01)
    }).ToList();

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task GetParties_FollowingNextCursor_ReturnsEveryRecordOnceInOrder(string sort)
    {
        var expected = OrderByNameThenId(_parties.Select(p => (p.Name!, p.Id)), sort);

        var actual = await WalkPagesAsync<PartyDto>("api/parties", sort, p => p.Id);

        actual.Should().Equal(expected);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task GetSites_FollowingNextCursor_ReturnsEveryRecordOnceInOrder(string sort)
    {
        var expected = OrderByNameThenId(_sites.Select(s => (s.Name!, s.Id)), sort);

        var actual = await WalkPagesAsync<SiteDto>("api/sites", sort, s => s.Id);

        actual.Should().Equal(expected);
    }

    [Theory]
    [InlineData("api/sites", "not-base-64!!!")]
    [InlineData("api/sites", "U21pdGh8")]
    [InlineData("api/parties", "not-base-64!!!")]
    [InlineData("api/parties", "U21pdGh8")]
    public async Task GetList_WithMalformedCursor_ReturnsBadRequest(string endpoint, string cursor)
    {
        var response = await _apiContainerFixture.HttpClient.GetAsync($"{endpoint}?page=2&cursor={HttpUtility.UrlEncode(cursor)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Cursor");
    }

    private async Task<List<string>> WalkPagesAsync<T>(string endpoint, string sort, Func<T, string> getId)
    {
        const int pageSize = 2;
        var ids = new List<string>();
        string? cursor = null;

        for (var i = 0; i <= Names.Length; i++)
        {
            var url = $"{endpoint}?order=name&sort={sort}&pageSize={pageSize}";
            if (cursor != null) url += $"&cursor={HttpUtility.UrlEncode(cursor)}";

            var response = await _apiContainerFixture.HttpClient.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

            var page = await response.Content.ReadFromJsonAsync<PaginatedResult<T>>();
            page.Should().NotBeNull();
            page!.TotalCount.Should().Be(Names.Length);
            ids.AddRange(page.Values.Select(getId));

            cursor = page.NextCursor;
            if (cursor == null) return ids;
        }

        throw new InvalidOperationException("Cursor pagination did not terminate.");
    }

    private static List<string> OrderByNameThenId(IEnumerable<(string Name, string Id)> records, string sort)
    {
        var ordered = sort == "desc"
            ? records.OrderByDescending(r => r.Name, StringComparer.Ordinal).ThenByDescending(r => r.Id, StringComparer.Ordinal)
            : records.OrderBy(r => r.Name, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal);

        return ordered.Select(r => r.Id).ToList();
    }

    public async Task InitializeAsync()
    {
        await _mongoDbFixture.MongoVerifier.DeleteAll<PartyDocument>();
        await _mongoDbFixture.MongoVerifier.DeleteAll<SiteDocument>();
        await _mongoDbFixture.MongoVerifier.Insert(_parties);
        await _mongoDbFixture.MongoVerifier.Insert(_sites);
    }

    public async Task DisposeAsync()
    {
        await _mongoDbFixture.MongoVerifier.Delete(_parties);
        await _mongoDbFixture.MongoVerifier.Delete(_sites);
    }
}
