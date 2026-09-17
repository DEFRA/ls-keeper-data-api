using FluentAssertions;
using KeeperData.Application.Queries.Cphs;
using KeeperData.Application.Queries.Cphs.Adapters;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.Cphs.Adapters;

public class CphsQueryAdapterTests
{
    private readonly Mock<ICphRepository> _repositoryMock;
    private readonly CphsQueryAdapter _sut;

    public CphsQueryAdapterTests()
    {
        _repositoryMock = new Mock<ICphRepository>();
        _sut = new CphsQueryAdapter(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetCphsAsync_WhenRepositoryReturnsItems_ReturnsThoseItems()
    {
        var expected = new List<CphDto>
        {
            new() { Cph = "01/001/0001" },
            new() { Cph = "02/002/0002" }
        };
        GivenRepositoryReturns(expected, totalCount: 2);

        var (items, _, _) = await WhenGettingCphs();

        items.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetCphsAsync_WhenRepositoryReturnsItems_ReturnsTotalCount()
    {
        GivenRepositoryReturns([new() { Cph = "01/001/0001" }], totalCount: 42);

        var (_, totalCount, _) = await WhenGettingCphs();

        totalCount.Should().Be(42);
    }

    [Fact]
    public async Task GetCphsAsync_NextCursorIsAlwaysNull()
    {
        GivenRepositoryReturns([new() { Cph = "01/001/0001" }], totalCount: 1);

        var (_, _, nextCursor) = await WhenGettingCphs(page: 1, pageSize: 10);

        nextCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetCphsAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyItemsAndZeroCount()
    {
        GivenRepositoryReturns([], totalCount: 0);

        var (items, totalCount, nextCursor) = await WhenGettingCphs();

        items.Should().BeEmpty();
        totalCount.Should().Be(0);
        nextCursor.Should().BeNull();
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 5)]
    [InlineData(3, 25)]
    public async Task GetCphsAsync_ForwardsPageAndPageSizeToRepository(int page, int pageSize)
    {
        GivenRepositoryReturns([], totalCount: 0);

        await WhenGettingCphs(page: page, pageSize: pageSize);

        _repositoryMock.Verify(r => r.GetPagedAsync(
            page,
            pageSize,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task GetCphsAsync_ForwardsSortToRepository(string? sort)
    {
        GivenRepositoryReturns([], totalCount: 0);

        await WhenGettingCphs(sort: sort);

        _repositoryMock.Verify(r => r.GetPagedAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            sort,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCphsAsync_ForwardsCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        GivenRepositoryReturns([], totalCount: 0);

        await _sut.GetCphsAsync(new GetCphsQuery(), cts.Token);

        _repositoryMock.Verify(r => r.GetPagedAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetCphsAsync_UsesDefaultPageAndPageSizeWhenNotSpecified()
    {
        GivenRepositoryReturns([], totalCount: 0);

        await _sut.GetCphsAsync(new GetCphsQuery());

        _repositoryMock.Verify(r => r.GetPagedAsync(
            1,
            10,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void GivenRepositoryReturns(List<CphDto> items, int totalCount)
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, totalCount));
    }

    private Task<(List<CphDto> Items, int TotalCount, string? NextCursor)> WhenGettingCphs(
        int page = 1,
        int pageSize = 10,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCphsQuery
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort
        };

        return _sut.GetCphsAsync(query, cancellationToken);
    }
}