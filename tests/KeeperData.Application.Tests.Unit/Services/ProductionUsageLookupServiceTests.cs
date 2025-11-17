using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class ProductionUsageLookupServiceTests
{
    private readonly Mock<IProductionUsageRepository> _mockRepository;
    private readonly ProductionUsageLookupService _sut;

    public ProductionUsageLookupServiceTests()
    {
        _mockRepository = new Mock<IProductionUsageRepository>();
        _sut = new ProductionUsageLookupService(_mockRepository.Object);
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
        _mockRepository
            .Setup(x => x.GetByIdAsync("test-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocument);

        // Act
        var result = await _sut.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().Be(expectedDocument);
        _mockRepository.Verify(x => x.GetByIdAsync("test-id", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetByIdAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductionUsageDocument?)null);

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingProductionUsage()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.FindAsync("APPROVED", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("APPROVED", "Approved Pyramid"));

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
        _mockRepository
            .Setup(r => r.FindAsync("Calf Rearer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("CALFREAR", "Calf Rearer"));

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
        _mockRepository
            .Setup(x => x.FindAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.productionUsageId.Should().BeNull();
        result.productionUsageName.Should().BeNull();
    }
}