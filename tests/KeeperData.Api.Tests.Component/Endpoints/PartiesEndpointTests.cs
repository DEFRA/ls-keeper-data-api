using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class PartiesEndpointTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture = appTestFixture;


    [Fact]
    public async Task GetParties_WithFilterAndSort_ReturnsFilteredAndSortedOkResult()
    {
        var parties = new List<PartyDocument> { CreateParty("Party A") };

        SetupRepository(parties, totalCount: 1);

        // TODO build query
        var query = ""; // $"?siteIdentifier=ID1&type=Type1&siteId={siteId}&keeperPartyId={keeperPartyId}&order=name&sort=asc";

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/parties{query}");

        // Assert //TODO
        await AssertPaginatedResponse(response, expectedCount: 1, expectedNames: ["Party A"]);
    }

    [Fact]
    public async Task GetParties_WithoutParameters_ReturnsDefaultOkResult()
    {
        // Arrange
        var parties = new List<PartyDocument> { CreateParty("Party A"), CreateParty("Party B") };

        SetupRepository(parties, totalCount: 2);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/parties");

        // Assert //TODO
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Party A", "Party B"]);
    }

    private static PartyDocument CreateParty(string name)
    {
        var party = new PartyDocument
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            State = PartyStatusType.Active.GetDescription()
        };

        return party;
    }

    private void SetupRepository(List<PartyDocument> sites, int totalCount)
    {
        _appTestFixture.AppWebApplicationFactory._partiesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sites);

        _appTestFixture.AppWebApplicationFactory._partiesRepositoryMock
            .Setup(r => r.CountAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);
    }

    private static async Task AssertPaginatedResponse(HttpResponseMessage response, int expectedCount, IEnumerable<string> expectedNames)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<PartyDocument>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(expectedCount);
        result.Values.Should().HaveCount(expectedCount);

        result.Values.Select(v => v.Name).Should().BeEquivalentTo(expectedNames);
    }
}