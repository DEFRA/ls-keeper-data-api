using FluentAssertions;
using KeeperData.Application.Queries.Pagination;

namespace KeeperData.Application.Tests.Unit.Queries.Pagination;

public class PaginatedResultTests
{
    [Fact]
    public void Map_ProjectsValues_AndPreservesPaginationMetadata()
    {
        var source = new PaginatedResult<int>
        {
            Count = 2,
            TotalCount = 10,
            Values = [1, 2],
            Page = 2,
            PageSize = 2,
            NextCursor = "cursor-123"
        };

        var mapped = source.Map(x => x.ToString());

        mapped.Values.Should().Equal("1", "2");
        mapped.Count.Should().Be(2);
        mapped.TotalCount.Should().Be(10);
        mapped.Page.Should().Be(2);
        mapped.PageSize.Should().Be(2);
        mapped.NextCursor.Should().Be("cursor-123");
    }

    [Fact]
    public void Map_WithEmptyValues_ReturnsEmptyResult()
    {
        var source = new PaginatedResult<int>
        {
            Count = 0,
            TotalCount = 0,
            Values = [],
            Page = 1,
            PageSize = 25
        };

        var mapped = source.Map(x => x * 2);

        mapped.Values.Should().BeEmpty();
        mapped.NextCursor.Should().BeNull();
    }
}