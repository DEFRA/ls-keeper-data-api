using FluentAssertions;
using KeeperData.Application.Queries.ReferenceSiteTypes;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.ReferenceSiteTypes;

public class GetReferenceSiteTypesQueryHandlerTests
{
    private readonly Mock<IReferenceDataCache> _cacheMock;
    private readonly GetReferenceSiteTypesQueryHandler _sut;

    public GetReferenceSiteTypesQueryHandlerTests()
    {
        _cacheMock = new Mock<IReferenceDataCache>();
        _sut = new GetReferenceSiteTypesQueryHandler(_cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnsSiteTypesOrderedBySortOrderThenName()
    {
        // Arrange
        var mockData = new List<SiteTypeDocument>
        {
            CreateDoc("3", "MA", "Market", 20),
            CreateDoc("1", "AH", "Agricultural Holding", 10),
            CreateDoc("2", "AC", "Assembly Centre", 10)
        };

        _cacheMock.Setup(c => c.SiteTypes).Returns(mockData);

        var query = new GetReferenceSiteTypesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(3);
        result.Values.Should().HaveCount(3);

        // Assert Ordering (AC7)
        result.Values[0].Name.Should().Be("Agricultural Holding");
        result.Values[1].Name.Should().Be("Assembly Centre");
        result.Values[2].Name.Should().Be("Market");
    }

    [Fact]
    public async Task Handle_WithLastUpdatedDateFilter_ReturnsOnlyModifiedSiteTypes()
    {
        // Arrange
        var filterDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockData = new List<SiteTypeDocument>
        {
            CreateDoc("1", "OLD", "Old", 10, filterDate.AddDays(-1)),
            CreateDoc("2", "NEW", "New", 20, filterDate.AddDays(1)),
            CreateDoc("3", "EXACT", "Exact", 30, filterDate)
        };

        _cacheMock.Setup(c => c.SiteTypes).Returns(mockData);

        var query = new GetReferenceSiteTypesQuery { LastUpdatedDate = filterDate };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(2);
        result.Values.Should().Contain(s => s.Code == "NEW");
        result.Values.Should().Contain(s => s.Code == "EXACT");
        result.Values.Should().NotContain(s => s.Code == "OLD");
    }

    [Fact]
    public async Task Handle_WhenNoSiteTypesExist_ReturnsEmptyList()
    {
        // Arrange
        _cacheMock.Setup(c => c.SiteTypes).Returns([]);

        var query = new GetReferenceSiteTypesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(0);
        result.Values.Should().BeEmpty();
    }

    private static SiteTypeDocument CreateDoc(string id, string code, string name, int sortOrder, DateTime? lastModified = null)
    {
        return new SiteTypeDocument
        {
            IdentifierId = id,
            Code = code,
            Name = name,
            SortOrder = sortOrder,
            LastModifiedDate = lastModified ?? DateTime.UtcNow
        };
    }
}