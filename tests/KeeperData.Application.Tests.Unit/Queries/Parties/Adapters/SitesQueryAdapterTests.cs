using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Sites;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Core.Documents;
using KeeperData.Core.DTOs;
using KeeperData.Core.Repositories;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Application.Tests.Unit.Queries.Sites.Adapters;

public class SitesQueryAdapterTests
{
    private readonly Mock<ISitesRepository> _repositoryMock;
    private readonly SitesQueryAdapter _adapter;

    public SitesQueryAdapterTests()
    {
        _repositoryMock = new Mock<ISitesRepository>();
        _repositoryMock.Setup(x => x.CountAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _adapter = new SitesQueryAdapter(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetSitesAsync_WithNoCursor_ShouldFallbackToSkip()
    {
        var query = new GetSitesQuery { Page = 2, PageSize = 10, Order = "name", Sort = "asc" };
        var expectedItems = new List<SiteDocument> { new() { Id = "1", Name = "A" } };

        _repositoryMock.Setup(x => x.CountAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<SortDefinition<SiteDocument>>(), 10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var (items, count, nextCursor) = await _adapter.GetSitesAsync(query);

        items.Should().HaveCount(1);
        items[0].Id.Should().Be("1");
        items[0].Name.Should().Be("A");
        count.Should().Be(1);
        nextCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetSitesAsync_WhenFullPageReturned_ShouldGenerateNextCursor()
    {
        var query = new GetSitesQuery { Page = 1, PageSize = 2, Order = "name", Sort = "asc" };
        var expectedItems = new List<SiteDocument>
        {
            new() { Id = "1", Name = "A" },
            new() { Id = "2", Name = "B" }
        };

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<SortDefinition<SiteDocument>>(), 0, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        var (items, count, nextCursor) = await _adapter.GetSitesAsync(query);

        nextCursor.Should().NotBeNull();
        var decoded = CursorHelper.Decode(nextCursor);
        decoded!.Value.id.Should().Be("2");
        decoded!.Value.sortValue.Should().Be("B");
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task GetSitesAsync_WithValidCursor_ShouldApplyCursorFilterAndIgnoreSkip(string sortDirection)
    {
        var cursor = CursorHelper.Encode("Farm", "123");
        var query = new GetSitesQuery { PageSize = 10, Cursor = cursor, Order = "name", Sort = sortDirection };
        var expectedItems = new List<SiteDocument> { new() { Id = "1", Name = "A" } };

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<SortDefinition<SiteDocument>>(), 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        await _adapter.GetSitesAsync(query);

        _repositoryMock.Verify(x => x.FindAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<SortDefinition<SiteDocument>>(), 0, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSitesAsync_WithInvalidCursor_ShouldIgnoreCursor()
    {
        var query = new GetSitesQuery { Page = 2, PageSize = 10, Cursor = "invalid-cursor-string" };
        var expectedItems = new List<SiteDocument>();

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<SortDefinition<SiteDocument>>(), 10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        await _adapter.GetSitesAsync(query);

        _repositoryMock.Verify(x => x.FindAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<SortDefinition<SiteDocument>>(), 10, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("type", "AH")]
    [InlineData("siteidentifier", "12/345")]
    [InlineData("unknown", "DefaultName")]
    public async Task GetSitesAsync_SortValues_ShouldMapCorrectly(string orderField, string expectedSortValue)
    {
        var query = new GetSitesQuery { PageSize = 1, Order = orderField };
        var item = new SiteDocument
        {
            Id = "999",
            Name = "DefaultName",
            Type = new SiteTypeSummaryDocument() { Code = "AH", IdentifierId = "x", Name = "x" },
            Identifiers = new List<SiteIdentifierDocument>
            {
                new() { Identifier = "12/345", IdentifierId = "y", Type = new SiteIdentifierSummaryDocument { Code = "y", IdentifierId = "y", Name = "y" } }
            }
        };

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<SortDefinition<SiteDocument>>(), 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SiteDocument> { item });

        var (_, _, nextCursor) = await _adapter.GetSitesAsync(query);

        var decoded = CursorHelper.Decode(nextCursor);
        decoded!.Value.sortValue.Should().Be(expectedSortValue);
    }

    [Fact]
    public async Task GetSitesAsync_SortValuesNullChecks_ShouldFallbackToEmptyString()
    {
        var query = new GetSitesQuery { PageSize = 1, Order = "type" };
        var item = new SiteDocument { Id = "999", Name = null!, Type = null, Identifiers = null! };

        _repositoryMock.Setup(x => x.FindAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<SortDefinition<SiteDocument>>(), 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SiteDocument> { item });

        var (_, _, nextCursor) = await _adapter.GetSitesAsync(query);

        var decoded = CursorHelper.Decode(nextCursor);
        decoded!.Value.sortValue.Should().Be(string.Empty);
    }
}
