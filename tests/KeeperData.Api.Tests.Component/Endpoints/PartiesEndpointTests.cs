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
        // Arrange
        var parties = new List<PartyDocument> { CreateParty("Reanu", "Keaves") };

        SetupRepositorySimple(parties);

        var query = "?firstName=Reanu&lastName=Keaves&email=reanu.keaves@skyrim.net&order=firstName&sort=asc";

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/parties{query}");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 1, expectedNames: ["Reanu Keaves"]);
    }

    [Fact]
    public async Task GetParties_WithoutParameters_ReturnsDefaultOkResult()
    {
        // Arrange
        var parties = new List<PartyDocument> { CreateParty("Donut Anne", "Chonk"), CreateParty("Zelda", "Hyrule") };

        SetupRepositorySimple(parties);

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync("/api/parties");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Donut Anne Chonk", "Zelda Hyrule"]);
    }

    [Fact]
    public async Task GetParties_WithPaginationParameters_ReturnsPagedResults()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();

        var parties = new List<PartyDocument>
        {
            CreateParty("Loafus", "Cramwell"),
            CreateParty("Buttlet", "Waterbeach"),
            CreateParty("Joe", "Bishop")
        };

        SetupRepositoryForPagination(parties, totalCount: 3, pageSize: 2);

        var query = "?page=1&pageSize=2";

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/parties{query}");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Buttlet Waterbeach", "Joe Bishop"]);
    }

    [Fact]
    public async Task GetParties_WithLastNameFilter_ReturnsFilteredResults()
    {
        // Arrange
        _appTestFixture.AppWebApplicationFactory.ResetMocks();

        var parties = new List<PartyDocument>
        {
            CreateParty("Mikasa", "Akerman"),
            CreateParty("Carl", "Dungeoneer"),
            CreateParty("Levi", "Akerman"),
            CreateParty("Reanu", "Keaves")
        };

        SetupRepositoryForFiltering(parties, "Akerman");

        var query = "?lastName=Akerman";

        // Act
        var response = await _appTestFixture.HttpClient.GetAsync($"/api/parties{query}");

        // Assert
        await AssertPaginatedResponse(response, expectedCount: 2, expectedNames: ["Mikasa Akerman", "Levi Akerman"]);
    }

    private static PartyDocument CreateParty(string firstName, string lastName)
    {
        var party = new PartyDocument
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = firstName,
            LastName = lastName,
            Name = $"{firstName} {lastName}",
            State = PartyStatusType.Active.GetDescription()
        };

        return party;
    }

    private void SetupRepositorySimple(List<PartyDocument> allParties)
    {
        var sortedParties = allParties.OrderBy(p => p.Name).ToList();

        _appTestFixture.AppWebApplicationFactory._partiesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MongoDB.Driver.FilterDefinition<PartyDocument> filter,
                          MongoDB.Driver.SortDefinition<PartyDocument> sort,
                          int skip,
                          int take,
                          CancellationToken cancellationToken) =>
            {
                var pagedParties = sortedParties.Skip(skip).Take(take).ToList();
                return pagedParties;
            });

        _appTestFixture.AppWebApplicationFactory._partiesRepositoryMock
            .Setup(r => r.CountAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allParties.Count);
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

    private static List<PartyDocument> ApplyFiltersAndSorting(List<PartyDocument> allParties, int expectedFilteredCount)
    {
        var baseFiltered = allParties.Where(p => !p.Deleted).OrderBy(p => p.Name).ToList();

        if (expectedFilteredCount < baseFiltered.Count)
        {
            var filtered = baseFiltered.Where(p => p.LastName == "Akerman").ToList();
            return filtered.Count == expectedFilteredCount ? filtered : baseFiltered.Take(expectedFilteredCount).ToList();
        }

        return baseFiltered;
    }

    private void SetupRepositoryForPagination(List<PartyDocument> allParties, int totalCount, int pageSize)
    {
        var sortedParties = allParties.OrderBy(p => p.Name).ToList();

        _appTestFixture.AppWebApplicationFactory._partiesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MongoDB.Driver.FilterDefinition<PartyDocument> filter,
                          MongoDB.Driver.SortDefinition<PartyDocument> sort,
                          int skip,
                          int take,
                          CancellationToken cancellationToken) =>
            {
                var pagedParties = sortedParties.Skip(skip).Take(take).ToList();
                return pagedParties;
            });

        _appTestFixture.AppWebApplicationFactory._partiesRepositoryMock
            .Setup(r => r.CountAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalCount);
    }

    private void SetupRepositoryForFiltering(List<PartyDocument> allParties, string lastName)
    {
        var filteredParties = allParties
            .Where(p => p.LastName == lastName)
            .OrderBy(p => p.Name)
            .ToList();

        _appTestFixture.AppWebApplicationFactory._partiesRepositoryMock
            .Setup(r => r.FindAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<MongoDB.Driver.SortDefinition<PartyDocument>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MongoDB.Driver.FilterDefinition<PartyDocument> filter,
                          MongoDB.Driver.SortDefinition<PartyDocument> sort,
                          int skip,
                          int take,
                          CancellationToken cancellationToken) =>
            {
                return filteredParties.Skip(skip).Take(take).ToList();
            });

        _appTestFixture.AppWebApplicationFactory._partiesRepositoryMock
            .Setup(r => r.CountAsync(
                It.IsAny<MongoDB.Driver.FilterDefinition<PartyDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(filteredParties.Count);
    }
}