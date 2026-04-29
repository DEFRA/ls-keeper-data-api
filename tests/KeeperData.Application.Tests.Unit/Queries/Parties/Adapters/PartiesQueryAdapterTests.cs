using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Parties;
using KeeperData.Application.Queries.Parties.Adapters;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.Parties.Adapters;

public class PartiesQueryAdapterTests
{
    private readonly Mock<IPartiesRepository> _repositoryMock;
    private readonly PartiesQueryAdapter _adapter;

    public PartiesQueryAdapterTests()
    {
        _repositoryMock = new Mock<IPartiesRepository>();
        _repositoryMock.Setup(x => x.CountAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _adapter = new PartiesQueryAdapter(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetPartiesAsync_WithNoCursor_ShouldFallbackToSkip()
    {
        var query = new GetPartiesQuery { Page = 2, PageSize = 10, Order = "name", Sort = "asc" };
        var documents = new List<PartyDocument> { new() { Id = "1", Name = "A" } };

        _repositoryMock.Setup(x => x.CountAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<SortDefinition<PartyDocument>>(), 10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        var (items, count, nextCursor) = await _adapter.GetPartiesAsync(query);

        items.Should().HaveCount(1);
        items[0].Id.Should().Be("1");
        items[0].Name.Should().Be("A");
        count.Should().Be(1);
        nextCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetPartiesAsync_WhenFullPageReturned_ShouldGenerateNextCursor()
    {
        var query = new GetPartiesQuery { Page = 1, PageSize = 2, Order = "name", Sort = "asc" };
        var expectedItems = new List<PartyDocument>
        {
            new() { Id = "1", Name = "A" },
            new() { Id = "2", Name = "B" }
        };

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<SortDefinition<PartyDocument>>(), 0, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var (items, count, nextCursor) = await _adapter.GetPartiesAsync(query);

        nextCursor.Should().NotBeNull();
        var decoded = CursorHelper.Decode(nextCursor);
        decoded!.Value.id.Should().Be("2");
        decoded!.Value.sortValue.Should().Be("B");
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task GetPartiesAsync_WithValidCursor_ShouldApplyCursorFilterAndIgnoreSkip(string sortDirection)
    {
        var cursor = CursorHelper.Encode("Smith", "123");
        var query = new GetPartiesQuery { PageSize = 10, Cursor = cursor, Order = "name", Sort = sortDirection };
        var documents = new List<PartyDocument> { new() { Id = "1", Name = "A" } };

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<SortDefinition<PartyDocument>>(), 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        var (items, count, nextCursor) = await _adapter.GetPartiesAsync(query);

        items.Should().HaveCount(1);
        items[0].Id.Should().Be("1");
        items[0].Name.Should().Be("A");
        // Verifies skip was 0
        _repositoryMock.Verify(x => x.FindAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<SortDefinition<PartyDocument>>(), 0, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPartiesAsync_WithInvalidCursor_ShouldIgnoreCursor()
    {
        var query = new GetPartiesQuery { Page = 2, PageSize = 10, Cursor = "invalid-cursor-string" };
        var expectedItems = new List<PartyDocument>();

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<SortDefinition<PartyDocument>>(), 10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        await _adapter.GetPartiesAsync(query);

        // Verifies skip fallback was used because cursor was invalid
        _repositoryMock.Verify(x => x.FindAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<SortDefinition<PartyDocument>>(), 10, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPartiesAsync_SortValues_ShouldMapCorrectly()
    {
        var query = new GetPartiesQuery { PageSize = 1, Order = "id" };
        var expectedItems = new List<PartyDocument> { new() { Id = "999", Name = "A" } };

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<SortDefinition<PartyDocument>>(), 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var (_, _, nextCursor) = await _adapter.GetPartiesAsync(query);

        var decoded = CursorHelper.Decode(nextCursor);
        decoded!.Value.sortValue.Should().Be("999");
    }

    [Fact]
    public async Task GetPartiesAsync_SortValuesNullName_ShouldFallbackToEmptyString()
    {
        var query = new GetPartiesQuery { PageSize = 1, Order = "unknown" };
        var expectedItems = new List<PartyDocument> { new() { Id = "999", Name = null } };

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<SortDefinition<PartyDocument>>(), 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var (_, _, nextCursor) = await _adapter.GetPartiesAsync(query);

        var decoded = CursorHelper.Decode(nextCursor);
        decoded!.Value.sortValue.Should().Be(string.Empty);
    }
}