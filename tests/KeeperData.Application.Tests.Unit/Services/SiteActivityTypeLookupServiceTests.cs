using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class SiteActivityTypeLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly SiteActivityTypeLookupService _sut;

    public SiteActivityTypeLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.SiteActivityTypes).Returns(Array.Empty<SiteActivityTypeDocument>());
        _sut = new SiteActivityTypeLookupService(_mockCache.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expectedDocument = new SiteActivityTypeDocument
        {
            IdentifierId = "test-id",
            Code = "MARP",
            Name = "Market on Paved Ground",
            IsActive = true,
            PriorityOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteActivityTypes).Returns(new[] { expectedDocument });

        // Act
        var result = await _sut.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().Be(expectedDocument);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockCache.Setup(c => c.SiteActivityTypes).Returns(Array.Empty<SiteActivityTypeDocument>());

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingSiteActivityType()
    {
        // Arrange
        var doc = new SiteActivityTypeDocument
        {
            IdentifierId = "MARP",
            Code = "MARP",
            Name = "Market on Paved Ground",
            IsActive = true,
            PriorityOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteActivityTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("MARP", CancellationToken.None);

        // Assert
        result.siteActivityTypeId.Should().Be("MARP");
        result.siteActivityTypeName.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithName_ReturnsMatchingSiteActivityType()
    {
        // Arrange
        var doc = new SiteActivityTypeDocument
        {
            IdentifierId = "CC",
            Code = "CC",
            Name = "Collection Centre",
            IsActive = true,
            PriorityOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteActivityTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("Collection Centre", CancellationToken.None);

        // Assert
        result.siteActivityTypeId.Should().Be("CC");
        result.siteActivityTypeName.Should().Be("Collection Centre");
    }

    [Fact]
    public async Task FindAsync_WhenNotFound_ReturnsNullTuple()
    {
        // Arrange
        _mockCache.Setup(c => c.SiteActivityTypes).Returns(Array.Empty<SiteActivityTypeDocument>());

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.siteActivityTypeId.Should().BeNull();
        result.siteActivityTypeName.Should().BeNull();
    }
}