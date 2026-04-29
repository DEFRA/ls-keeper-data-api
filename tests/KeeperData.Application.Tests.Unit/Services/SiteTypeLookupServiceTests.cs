using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class SiteTypeLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly SiteTypeLookupService _sut;

    public SiteTypeLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.SiteTypes).Returns(Array.Empty<SiteTypeDocument>());
        _sut = new SiteTypeLookupService(_mockCache.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expectedDocument = new SiteTypeDocument
        {
            IdentifierId = "test-id",
            Code = "AC",
            Name = "Assembly Centre",
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteTypes).Returns(new[] { expectedDocument });

        // Act
        var result = await _sut.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().Be(expectedDocument);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockCache.Setup(c => c.SiteTypes).Returns(Array.Empty<SiteTypeDocument>());

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingSiteType()
    {
        // Arrange
        var doc = new SiteTypeDocument
        {
            IdentifierId = "AC",
            Code = "AC",
            Name = "Assembly Centre",
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("AC", CancellationToken.None);

        // Assert
        result.siteTypeId.Should().Be("AC");
        result.siteTypeName.Should().Be("Assembly Centre");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithName_ReturnsMatchingSiteType()
    {
        // Arrange
        var doc = new SiteTypeDocument
        {
            IdentifierId = "MA",
            Code = "MA",
            Name = "Market",
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("Market", CancellationToken.None);

        // Assert
        result.siteTypeId.Should().Be("MA");
        result.siteTypeName.Should().Be("Market");
    }

    [Fact]
    public async Task FindAsync_WhenNotFound_ReturnsNullTuple()
    {
        // Arrange
        _mockCache.Setup(c => c.SiteTypes).Returns(Array.Empty<SiteTypeDocument>());

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.siteTypeId.Should().BeNull();
        result.siteTypeName.Should().BeNull();
    }
}