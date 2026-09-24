using FluentAssertions;
using KeeperData.Application;
using KeeperData.Application.Queries.Holdings;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class HoldingsEndpointTests : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture;
    private readonly Mock<IReadModelSqliteCacheService> _mockCache;
    private readonly Mock<IRequestExecutor> _mockExecutor;
    private readonly HttpClient _client;
    private readonly HttpClient _unauthenticatedClient;

    public HoldingsEndpointTests(AppTestFixture appTestFixture)
    {
        _appTestFixture = appTestFixture;

        _mockCache = new Mock<IReadModelSqliteCacheService>();
        _mockExecutor = new Mock<IRequestExecutor>();

        var factory = _appTestFixture.AppWebApplicationFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_mockCache.Object);
                services.AddSingleton(_mockExecutor.Object);
            });
        });

        _client = factory.CreateClient();
        _client.AddBasicApiKey("ApiKey", "integration-test-secret");

        _unauthenticatedClient = factory.CreateClient();
    }

    [Fact]
    public async Task GetHoldingDetail_WhenCacheNotLoaded_Returns503ProblemDetails()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(false);

        // Act
        var response = await _client.GetAsync("/api/v2/holdings/13/169/0007");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(503);
        problemDetails.Detail.Should().Be("The SAM read model is not cached locally, so holding details cannot be resolved.");

        _mockExecutor.Verify(x => x.ExecuteQuery(It.IsAny<GetHoldingDetailQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("1", "169", "0007", "county")]
    [InlineData("123", "169", "0007", "county")]
    [InlineData("ab", "169", "0007", "county")]
    [InlineData("13", "16", "0007", "parish")]
    [InlineData("13", "1699", "0007", "parish")]
    [InlineData("13", "abc", "0007", "parish")]
    [InlineData("13", "169", "7", "holding")]
    [InlineData("13", "169", "00007", "holding")]
    [InlineData("13", "169", "abcd", "holding")]
    public async Task GetHoldingDetail_WhenSegmentFailsConstraint_Returns400ValidationProblemDetails(
        string county, string parish, string holding, string expectedErrorField)
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(true);

        // Act
        var response = await _client.GetAsync($"/api/v2/holdings/{county}/{parish}/{holding}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Errors.Should().ContainKey(expectedErrorField);

        _mockExecutor.Verify(x => x.ExecuteQuery(It.IsAny<GetHoldingDetailQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetHoldingDetail_WhenHoldingExists_Returns200WithDocumentedShape()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(true);

        var expectedDetail = new HoldingDetail(
            Identifier: "13/169/0007",
            HoldingType: "permanent",
            Name: "Land At Test Farm 06",
            StartDate: DateTimeOffset.Parse("2026-03-09T00:00:00Z"),
            EndDate: null,
            Location: new HoldingLocation(
                OsMapReference: "TL9560015900",
                Easting: 595600,
                Northing: 215900,
                Address: new HoldingAddress(
                    Udprn: null,
                    AddressLine1: "Test Farm 06",
                    AddressLine2: "Layer Road",
                    PostTown: "COLCHESTER",
                    Locality: "Great Wigborough",
                    Postcode: "CO5 7RR",
                    Country: "England")),
            Associations:
            [
                new HoldingAssociation(
                    CustomerNumber: "C161215",
                    Title: "MRS",
                    FirstName: "Sheila",
                    LastName: "Keeper-Six",
                    Name: "MRS Sheila F Keeper-Six",
                    PartyType: "person",
                    Email: "krds-test06@livestockinformationb2cqa.onmicrosoft.com",
                    Mobile: null,
                    Telephone: "01206 999999",
                    Roles:
                    [
                        new HoldingRole("holder", []),
                        new HoldingRole("keeper", ["CTT"]),
                        new HoldingRole("owner", ["CTT"])
                    ])
            ],
            AllowedSpecies: ["CHK", "CTT", "PG", "SHP"],
            Marks:
            [
                new HoldingMark("240728", DateTimeOffset.Parse("2008-07-16T00:00:00Z"), null, ["CTT"])
            ]);

        _mockExecutor
            .Setup(x => x.ExecuteQuery(
                It.Is<GetHoldingDetailQuery>(q => q.County == "13" && q.Parish == "169" && q.Holding == "0007" && q.Cph == "13/169/0007"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDetail);

        // Act
        var response = await _client.GetAsync("/api/v2/holdings/13/169/0007");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<HoldingDetail>();
        result.Should().NotBeNull();
        result!.Identifier.Should().Be("13/169/0007");
        result.HoldingType.Should().Be("permanent");
        result.Name.Should().Be("Land At Test Farm 06");
        result.Location.Address.AddressLine1.Should().Be("Test Farm 06");
        result.Location.Address.AddressLine2.Should().Be("Layer Road");
        result.Associations.Should().HaveCount(1);
        result.Associations[0].Roles.Should().HaveCount(3);
        result.AllowedSpecies.Should().BeEquivalentTo(["CHK", "CTT", "PG", "SHP"]);
        result.Marks.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHoldingDetail_WhenHoldingNotFound_Returns404ProblemDetails()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(true);

        _mockExecutor
            .Setup(x => x.ExecuteQuery(It.IsAny<GetHoldingDetailQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Holding with CPH '13/169/0007' was not found."));

        // Act
        var response = await _client.GetAsync("/api/v2/holdings/13/169/0007");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHoldingDetail_WhenUnauthenticated_Returns401Unauthorized()
    {
        // Act
        var response = await _unauthenticatedClient.GetAsync("/api/v2/holdings/13/169/0007");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHoldings_WhenCacheNotLoaded_Returns503ProblemDetails()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(false);

        // Act
        var response = await _client.GetAsync("/api/v2/holdings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(503);
        problemDetails.Detail.Should().Be("The SAM read model is not cached locally, so holding details cannot be resolved.");

        _mockExecutor.Verify(x => x.ExecuteQuery(It.IsAny<GetHoldingsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetHoldings_WhenUnauthenticated_Returns401Unauthorized()
    {
        // Act
        var response = await _unauthenticatedClient.GetAsync("/api/v2/holdings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("?page=-1&pageSize=0", "page")]
    [InlineData("?page=-1&pageSize=0", "pageSize")]
    [InlineData("?page=0", "page")]
    [InlineData("?pageSize=0", "pageSize")]
    [InlineData("?pageSize=200", "pageSize")]
    [InlineData("?sort=invalid", "sort")]
    [InlineData("?order=unsupported", "order")]
    public async Task GetHoldings_WhenInvalidParameters_Returns400ValidationProblemDetails(string queryString, string expectedErrorField)
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(true);

        // Act
        var response = await _client.GetAsync($"/api/v2/holdings{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(400);
        problemDetails.Errors.Should().ContainKey(expectedErrorField);

        _mockExecutor.Verify(x => x.ExecuteQuery(It.IsAny<GetHoldingsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetHoldings_DefaultFirstPage_Returns200WithPaginatedResult()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        var dataTimestamp = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        _mockCache.Setup(c => c.DataTimestamp).Returns(dataTimestamp);

        var sampleValues = Enumerable.Range(1, 10).Select(i => new HoldingDetail(
            Identifier: $"13/169/{i:D4}",
            HoldingType: "permanent",
            Name: $"Farm {i}",
            StartDate: DateTimeOffset.Parse("2026-03-09T00:00:00Z"),
            EndDate: null,
            Location: new HoldingLocation(null, null, null, new HoldingAddress(null, null, null, null, null, null, null)),
            Associations: [],
            AllowedSpecies: ["CTT"],
            Marks: []
        )).ToList();

        var paginatedResult = new PaginatedResult<HoldingDetail>
        {
            Count = 10,
            TotalCount = 15420,
            DataTimestamp = dataTimestamp,
            Page = 1,
            PageSize = 10,
            Values = sampleValues
        };

        _mockExecutor
            .Setup(x => x.ExecuteQuery(
                It.Is<GetHoldingsQuery>(q => q.Page == 1 && q.PageSize == 10 && q.Sort == "asc" && q.Order == "cph"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var response = await _client.GetAsync("/api/v2/holdings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Data-Timestamp").Should().ContainSingle().Which.Should().Be(dataTimestamp.ToString("o"));

        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<HoldingDetail>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(10);
        result.TotalCount.Should().Be(15420);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1542);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.Values.Should().HaveCount(10);
        result.Values[0].Identifier.Should().Be("13/169/0001");
        result.Values[9].Identifier.Should().Be("13/169/0010");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetHoldings_WhenCacheRefreshesDuringQuery_UsesOnlyTheQueriedSnapshotTimestamp(bool hasTimestamp)
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        var timestamp = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        _mockCache.Setup(c => c.DataTimestamp).Returns(timestamp);
        var queryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queryResult = new TaskCompletionSource<PaginatedResult<HoldingDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockExecutor
            .Setup(x => x.ExecuteQuery(It.IsAny<GetHoldingsQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                queryStarted.SetResult();
                return queryResult.Task;
            });

        var responseTask = _client.GetAsync("/api/v2/holdings");
        await queryStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _mockCache.Setup(c => c.DataTimestamp).Returns(timestamp.AddDays(1));
        queryResult.SetResult(new PaginatedResult<HoldingDetail>
        {
            Page = 1,
            PageSize = 10,
            DataTimestamp = hasTimestamp ? timestamp : null
        });
        var response = await responseTask;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        if (hasTimestamp)
        {
            response.Headers.GetValues("X-Data-Timestamp").Should().Equal(timestamp.ToString("o"));
        }
        else
        {
            response.Headers.Contains("X-Data-Timestamp").Should().BeFalse();
        }
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("dataTimestamp");
        _mockCache.VerifyGet(c => c.DataTimestamp, Times.Never);
    }

    [Fact]
    public async Task GetHoldings_CustomPageAndSort_Returns200WithCustomPagination()
    {
        // Arrange
        _mockCache.Setup(c => c.IsLoaded).Returns(true);

        var sampleValues = Enumerable.Range(1, 5).Select(i => new HoldingDetail(
            Identifier: $"13/169/{10 - i:D4}",
            HoldingType: "permanent",
            Name: $"Farm {10 - i}",
            StartDate: null,
            EndDate: null,
            Location: new HoldingLocation(null, null, null, new HoldingAddress(null, null, null, null, null, null, null)),
            Associations: [],
            AllowedSpecies: [],
            Marks: []
        )).ToList();

        var paginatedResult = new PaginatedResult<HoldingDetail>
        {
            Count = 5,
            TotalCount = 20,
            Page = 2,
            PageSize = 5,
            Values = sampleValues
        };

        _mockExecutor
            .Setup(x => x.ExecuteQuery(
                It.Is<GetHoldingsQuery>(q => q.Page == 2 && q.PageSize == 5 && q.Sort == "desc" && q.Order == "name"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var response = await _client.GetAsync("/api/v2/holdings?page=2&pageSize=5&sort=desc&order=name");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<HoldingDetail>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Values.Should().HaveCount(5);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }
}