using FluentAssertions;
using KeeperData.Api.Tests.Integration.Fixtures;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace KeeperData.Api.Tests.Integration.Endpoints;

[Collection("Integration"), Trait("Dependence", "testcontainers")]
public class HoldingsEndpointTests(ApiContainerFixture fixture)
{
    private readonly ApiContainerFixture _fixture = fixture;

    [Fact]
    public async Task WhenHoldingsEndpointCalledWithSearch_ReturnsMatchingHoldingAndPagination()
    {
        var response = await _fixture.HttpClient.GetAsync("api/v2/holdings?search=green%20fields&page=1&pageSize=1&order=cph&sort=asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<HoldingDetail>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.TotalCount.Should().Be(1);
        result.Count.Should().Be(1);
        result.Values.Should().ContainSingle().Which.Identifier.Should().Be("13/169/0007");
    }

    [Fact]
    public async Task WhenHoldingsSearchReturnsMultipleMatches_PaginatesInCphOrder()
    {
        var response = await _fixture.HttpClient.GetAsync("api/v2/holdings?search=farm&page=2&pageSize=1&order=cph&sort=asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<HoldingDetail>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.TotalCount.Should().Be(336);
        result.Count.Should().Be(1);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result.Values.Should().ContainSingle().Which.Identifier.Should().Be("13/169/0007");
    }

    [Fact]
    public async Task WhenThirtyFourHoldingsMatch_ReturnsTwoUniquePagesOfTwentyFiveAndNine()
    {
        var firstResponse = await _fixture.HttpClient.GetAsync("api/v2/holdings?search=QAThirtyFour&page=1&pageSize=25&order=cph&sort=asc");
        var secondResponse = await _fixture.HttpClient.GetAsync("api/v2/holdings?search=QAThirtyFour&page=2&pageSize=25&order=cph&sort=asc");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = (await firstResponse.Content.ReadFromJsonAsync<PaginatedResult<HoldingDetail>>())!;
        var second = (await secondResponse.Content.ReadFromJsonAsync<PaginatedResult<HoldingDetail>>())!;

        first.Count.Should().Be(25);
        first.Values.Should().HaveCount(25);
        first.Page.Should().Be(1);
        first.PageSize.Should().Be(25);
        first.TotalCount.Should().Be(34);
        first.TotalPages.Should().Be(2);
        first.HasPreviousPage.Should().BeFalse();
        first.HasNextPage.Should().BeTrue();

        second.Count.Should().Be(9);
        second.Values.Should().HaveCount(9);
        second.Page.Should().Be(2);
        second.PageSize.Should().Be(25);
        second.TotalCount.Should().Be(34);
        second.TotalPages.Should().Be(2);
        second.HasPreviousPage.Should().BeTrue();
        second.HasNextPage.Should().BeFalse();

        var identifiers = first.Values.Concat(second.Values).Select(holding => holding.Identifier);
        identifiers.Should().OnlyHaveUniqueItems().And.BeInAscendingOrder().And.HaveCount(34);
    }

    [Fact]
    public async Task WhenHoldingsEndpointCalledWithInvalidSearchSyntax_ReturnsBadRequest()
    {
        var response = await _fixture.HttpClient.GetAsync("api/v2/holdings?search=green*");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenHoldingsEndpointCalledWithoutSearch_ReturnsAllSeededHoldings()
    {
        var response = await _fixture.HttpClient.GetAsync("api/v2/holdings?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<HoldingDetail>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(344);
        result.Values.Should().HaveCount(10);
    }
}