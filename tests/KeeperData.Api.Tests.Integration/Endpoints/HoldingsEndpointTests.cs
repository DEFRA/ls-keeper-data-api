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
        result.TotalCount.Should().Be(7);
        result.Count.Should().Be(1);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
        result.Values.Should().ContainSingle().Which.Identifier.Should().Be("22/100/0001");
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
        result!.TotalCount.Should().Be(15);
        result.Values.Should().HaveCount(10);
    }
}