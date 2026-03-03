using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class ProductionUsageLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly ProductionUsageLookupService _sut;

    public ProductionUsageLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.ProductionUsages).Returns(Array.Empty<ProductionUsageDocument>());
        _sut = new ProductionUsageLookupService(_mockCache.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expectedDocument = new ProductionUsageDocument
        {
            IdentifierId = "test-id",
            Code = "APPROVED",
            Description = "Approved Pyramid",
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.ProductionUsages).Returns(new[] { expectedDocument });

        // Act
        var result = await _sut.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().Be(expectedDocument);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockCache.Setup(c => c.ProductionUsages).Returns(Array.Empty<ProductionUsageDocument>());

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingProductionUsage()
    {
        // Arrange
        var doc = new ProductionUsageDocument
        {
            IdentifierId = "APPROVED",
            Code = "APPROVED",
            Description = "Approved Pyramid",
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.ProductionUsages).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("APPROVED", CancellationToken.None);

        // Assert
        result.productionUsageId.Should().Be("APPROVED");
        result.productionUsageName.Should().Be("Approved Pyramid");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDescription_ReturnsMatchingProductionUsage()
    {
        // Arrange
        var doc = new ProductionUsageDocument
        {
            IdentifierId = "CALFREAR",
            Code = "CALFREAR",
            Description = "Calf Rearer",
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.ProductionUsages).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("Calf Rearer", CancellationToken.None);

        // Assert
        result.productionUsageId.Should().Be("CALFREAR");
        result.productionUsageName.Should().Be("Calf Rearer");
    }

    [Fact]
    public async Task FindAsync_WhenNotFound_ReturnsNullTuple()
    {
        // Arrange
        _mockCache.Setup(c => c.ProductionUsages).Returns(Array.Empty<ProductionUsageDocument>());

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.productionUsageId.Should().BeNull();
        result.productionUsageName.Should().BeNull();
    }
}