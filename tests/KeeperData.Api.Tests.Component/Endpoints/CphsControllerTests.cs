using FluentAssertions;
using KeeperData.Api.Controllers;
using KeeperData.Api.Controllers.RequestDtos.Cphs;
using KeeperData.Application;
using KeeperData.Application.Queries.Cphs;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KeeperData.Api.Tests.Component.Endpoints;

public class CphsControllerTests
{
    private readonly Mock<IRequestExecutor> _mockExecutor;
    private readonly Mock<ICphSqliteCacheService> _mockCache;
    private readonly CphsController _controller;

    public CphsControllerTests()
    {
        _mockExecutor = new Mock<IRequestExecutor>();
        _mockCache = new Mock<ICphSqliteCacheService>();
        _controller = new CphsController(_mockExecutor.Object, _mockCache.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetCphs_WhenCacheNotLoaded_Returns503()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(false);
        var request = new GetCphsRequest();

        var result = await _controller.GetCphs(request);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task GetCphs_WhenLoaded_Returns200WithPaginatedResult()
    {
        var paginatedResult = new PaginatedResult<CphDto>
        {
            Count = 3,
            TotalCount = 100,
            Values = [new() { Cph = "01/001/0001" }, new() { Cph = "01/001/0002" }, new() { Cph = "01/001/0003" }],
            Page = 1,
            PageSize = 10
        };

        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockCache.Setup(c => c.DataTimestamp).Returns(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        _mockExecutor.Setup(e => e.ExecuteQuery(It.IsAny<GetCphsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        var request = new GetCphsRequest();
        var result = await _controller.GetCphs(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var paginated = okResult.Value.Should().BeOfType<PaginatedResult<CphDto>>().Subject;
        paginated.Count.Should().Be(3);
        paginated.TotalCount.Should().Be(100);
        paginated.Page.Should().Be(1);
        paginated.PageSize.Should().Be(10);
        paginated.Values.Should().HaveCount(3);
        paginated.Values[0].Cph.Should().Be("01/001/0001");
    }

    [Fact]
    public async Task GetCphs_DefaultsPage1AndPageSize10()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockExecutor.Setup(e => e.ExecuteQuery(It.IsAny<GetCphsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<CphDto>());

        var request = new GetCphsRequest();
        await _controller.GetCphs(request);

        _mockExecutor.Verify(e => e.ExecuteQuery(
            It.Is<GetCphsQuery>(q => q.Page == 1 && q.PageSize == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCphs_ClampsPageSizeToMax100()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockExecutor.Setup(e => e.ExecuteQuery(It.IsAny<GetCphsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<CphDto>());

        var request = new GetCphsRequest { PageSize = 500 };
        await _controller.GetCphs(request);

        _mockExecutor.Verify(e => e.ExecuteQuery(
            It.Is<GetCphsQuery>(q => q.PageSize == 100),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCphs_ClampsPageSizeMinTo1()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockExecutor.Setup(e => e.ExecuteQuery(It.IsAny<GetCphsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<CphDto>());

        var request = new GetCphsRequest { PageSize = 0 };
        await _controller.GetCphs(request);

        _mockExecutor.Verify(e => e.ExecuteQuery(
            It.Is<GetCphsQuery>(q => q.PageSize == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCphs_PassesOrderAndSort()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockExecutor.Setup(e => e.ExecuteQuery(It.IsAny<GetCphsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<CphDto>());

        var request = new GetCphsRequest { Page = 2, PageSize = 20, Order = "cph", Sort = "desc" };
        await _controller.GetCphs(request);

        _mockExecutor.Verify(e => e.ExecuteQuery(
            It.Is<GetCphsQuery>(q => q.Page == 2 && q.PageSize == 20 && q.Order == "cph" && q.Sort == "desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCphs_SetsXDataTimestampHeader()
    {
        var timestamp = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockCache.Setup(c => c.DataTimestamp).Returns(timestamp);
        _mockExecutor.Setup(e => e.ExecuteQuery(It.IsAny<GetCphsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<CphDto>());

        var request = new GetCphsRequest();
        await _controller.GetCphs(request);

        _controller.Response.Headers["X-Data-Timestamp"].ToString().Should().Contain("2026-06-30");
    }

    [Fact]
    public async Task GetCphs_WhenNoTimestamp_DoesNotSetHeader()
    {
        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockCache.Setup(c => c.DataTimestamp).Returns((DateTime?)null);
        _mockExecutor.Setup(e => e.ExecuteQuery(It.IsAny<GetCphsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<CphDto>());

        var request = new GetCphsRequest();
        await _controller.GetCphs(request);

        _controller.Response.Headers.ContainsKey("X-Data-Timestamp").Should().BeFalse();
    }

    [Fact]
    public async Task GetCphs_PaginationMetadata_IsCorrect()
    {
        var paginatedResult = new PaginatedResult<CphDto>
        {
            Count = 1,
            TotalCount = 50,
            Values = [new() { Cph = "01/001/0001" }],
            Page = 1,
            PageSize = 20
        };

        _mockCache.Setup(c => c.IsLoaded).Returns(true);
        _mockExecutor.Setup(e => e.ExecuteQuery(It.IsAny<GetCphsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        var request = new GetCphsRequest { PageSize = 20 };
        var result = await _controller.GetCphs(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var paginated = okResult.Value.Should().BeOfType<PaginatedResult<CphDto>>().Subject;
        paginated.TotalPages.Should().Be(3);
        paginated.HasNextPage.Should().BeTrue();
        paginated.HasPreviousPage.Should().BeFalse();
    }
}