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

public class PartiesEndpointTests
{
    private readonly Mock<IPartiesRepository> _partiesRepositoryMock = new();
    private readonly HttpClient _client;

    public PartiesEndpointTests()
    {
        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsScoped(_partiesRepositoryMock.Object);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetParties_WithFilterAndSort_ReturnsFilteredAndSortedOkResult()
    {
        var parties = new List<PartyDocument> { CreateParty("Party A") };
        SetupRepository(parties, totalCount: 1);

        var response = await _client.GetAsync($"/api/party");

        await AssertPaginatedResponse(response, expectedCount: 1, expectedNames: ["Party A"]);
    }

    [Fact]
    public async Task GetParties_WithoutParameters_ReturnsDefaultOkResult()
    {
        var parties = new List<PartyDocument> { CreateParty("Party A"), CreateParty("Party B") };

        SetupRepository(parties, totalCount: 2);

        var response = await _client.GetAsync("/api/party");

        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Party A", "Party B"]);
    }

    private static PartyDocument CreateParty(string name)
    {
        var party = new PartyDocument
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            State = "Active"
        };

        return party;
    }

    private void SetupRepository(List<PartyDocument> sites, int totalCount)
    {
        _partiesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sites);

        _partiesRepositoryMock
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