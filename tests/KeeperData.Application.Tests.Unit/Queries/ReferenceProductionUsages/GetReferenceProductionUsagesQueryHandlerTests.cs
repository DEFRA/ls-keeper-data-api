using FluentAssertions;
using KeeperData.Application.Queries.ReferenceProductionUsages;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;
using Xunit;

namespace KeeperData.Application.Tests.Unit.Queries.ReferenceProductionUsages;

public class GetReferenceProductionUsagesQueryHandlerTests
{
    private readonly Mock<IReferenceDataCache> _cacheMock;
    private readonly GetReferenceProductionUsagesQueryHandler _sut;

    public GetReferenceProductionUsagesQueryHandlerTests()
    {
        _cacheMock = new Mock<IReferenceDataCache>();
        _sut = new GetReferenceProductionUsagesQueryHandler(_cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalled_ReturnsProductionUsagesOrderedByDescriptionThenCode()
    {
        // Arrange
        var mockData = new List<ProductionUsageDocument>
        {
            CreateDoc("3", "MEAT", "Meat"),
            CreateDoc("1", "BEEF", "Beef"),
            CreateDoc("2", "DAIRY", "Dairy")
        };

        _cacheMock.Setup(c => c.ProductionUsages).Returns(mockData);

        var query = new GetReferenceProductionUsagesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(3);
        result.Values.Should().HaveCount(3);

        result.Values[0].Description.Should().Be("Beef");
        result.Values[1].Description.Should().Be("Dairy");
        result.Values[2].Description.Should().Be("Meat");
    }

    [Fact]
    public async Task Handle_WithLastUpdatedDateFilter_ReturnsOnlyModifiedProductionUsages()
    {
        // Arrange
        var filterDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockData = new List<ProductionUsageDocument>
        {
            CreateDoc("1", "OLD", "Old", filterDate.AddDays(-1)),
            CreateDoc("2", "NEW", "New", filterDate.AddDays(1)),
            CreateDoc("3", "EXACT", "Exact", filterDate)
        };

        _cacheMock.Setup(c => c.ProductionUsages).Returns(mockData);

        var query = new GetReferenceProductionUsagesQuery { LastUpdatedDate = filterDate };

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(2);
        result.Values.Should().Contain(p => p.Code == "NEW");
        result.Values.Should().Contain(p => p.Code == "EXACT");
        result.Values.Should().NotContain(p => p.Code == "OLD");
    }

    [Fact]
    public async Task Handle_WhenNoProductionUsagesExist_ReturnsEmptyList()
    {
        // Arrange
        _cacheMock.Setup(c => c.ProductionUsages).Returns([]);

        var query = new GetReferenceProductionUsagesQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(0);
        result.Values.Should().BeEmpty();
    }

    private static ProductionUsageDocument CreateDoc(string id, string code, string description, DateTime? lastModified = null)
    {
        return new ProductionUsageDocument
        {
            IdentifierId = id,
            Code = code,
            Description = description,
            LastModifiedDate = lastModified ?? DateTime.UtcNow
        };
    }
}