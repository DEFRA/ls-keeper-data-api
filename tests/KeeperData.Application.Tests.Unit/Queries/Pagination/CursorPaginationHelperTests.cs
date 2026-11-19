using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Repositories;

namespace KeeperData.Application.Tests.Unit.Queries.Pagination;

public class CursorPaginationHelperTests
{
    private sealed record TestEntity(string? Id, string Name) : IEntity;

    [Fact]
    public void GetNextCursor_WithFullPage_ShouldEncodeLastItem()
    {
        var items = new List<TestEntity> { new("1", "Alpha"), new("2", "Bravo") };

        var cursor = CursorPaginationHelper.GetNextCursor(items, 2, x => x.Name);

        CursorHelper.Decode(cursor).Should().Be(("Bravo", "2"));
    }

    [Fact]
    public void GetNextCursor_WithPartialPage_ShouldReturnNull()
    {
        var items = new List<TestEntity> { new("1", "Alpha") };

        CursorPaginationHelper.GetNextCursor(items, 2, x => x.Name).Should().BeNull();
    }

    [Fact]
    public void GetNextCursor_WhenLastItemHasNoId_ShouldThrow()
    {
        var items = new List<TestEntity> { new("1", "Alpha"), new(null, "Bravo") };

        var act = () => CursorPaginationHelper.GetNextCursor(items, 2, x => x.Name);

        act.Should().Throw<InvalidOperationException>();
    }
}