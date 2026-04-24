using FluentAssertions;
using KeeperData.Application.Queries.ReferenceActivities;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.ReferenceActivities;

public class GetReferenceActivitiesQueryHandlerTests
{
    private readonly Mock<IReferenceDataCache> _cacheMock;
    private readonly GetReferenceActivitiesQueryHandler _sut;

    public GetReferenceActivitiesQueryHandlerTests()
    {
        _cacheMock = new Mock<IReferenceDataCache>();
        _sut = new GetReferenceActivitiesQueryHandler(_cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnsActivitiesOrderedByPriorityOrderThenName()
    {
        // Arrange
        var mockData = new List<SiteActivityTypeDocument>
        {
            CreateDoc("3", "CC", "Collection Centre", 30),
            CreateDoc("1", "MARP", "Market on Paved Ground", 10),
            CreateDoc("2", "MARU", "Market on Unpaved Ground", 20)
        };

        _cacheMock.Setup(c => c.SiteActivityTypes).Returns(mockData);

        var query = new GetReferenceActivitiesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(3);
        result.Values.Should().HaveCount(3);

        result.Values[0].Name.Should().Be("Market on Paved Ground");   // PriorityOrder 10
        result.Values[1].Name.Should().Be("Market on Unpaved Ground"); // PriorityOrder 20
        result.Values[2].Name.Should().Be("Collection Centre");        // PriorityOrder 30
    }

    [Fact]
    public async Task Handle_WithLastUpdatedDateFilter_ReturnsOnlyModifiedActivities()
    {
        // Arrange
        var filterDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockData = new List<SiteActivityTypeDocument>
        {
            CreateDoc("1", "OLD", "Old", 10, filterDate.AddDays(-1)),
            CreateDoc("2", "NEW", "New", 20, filterDate.AddDays(1)),
            CreateDoc("3", "EXACT", "Exact", 30, filterDate)
        };

        _cacheMock.Setup(c => c.SiteActivityTypes).Returns(mockData);

        var query = new GetReferenceActivitiesQuery { LastUpdatedDate = filterDate };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(2);
        result.Values.Should().Contain(s => s.Code == "NEW");
        result.Values.Should().Contain(s => s.Code == "EXACT");
        result.Values.Should().NotContain(s => s.Code == "OLD");
    }

    [Fact]
    public async Task Handle_WhenNoActivitiesExist_ReturnsEmptyList()
    {
        // Arrange
        _cacheMock.Setup(c => c.SiteActivityTypes).Returns([]);

        var query = new GetReferenceActivitiesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(0);
        result.Values.Should().BeEmpty();
    }

    private static SiteActivityTypeDocument CreateDoc(string id, string code, string name, int priorityOrder, DateTime? lastModified = null)
    {
        return new SiteActivityTypeDocument
        {
            IdentifierId = id,
            Code = code,
            Name = name,
            PriorityOrder = priorityOrder,
            LastModifiedDate = lastModified ?? DateTime.UtcNow
        };
    }
}