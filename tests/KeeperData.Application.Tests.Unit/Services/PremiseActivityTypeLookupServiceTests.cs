using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class PremiseActivityTypeLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly PremiseActivityTypeLookupService _sut;

    public PremiseActivityTypeLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.PremisesActivityTypes).Returns(Array.Empty<PremisesActivityTypeDocument>());
        _sut = new PremiseActivityTypeLookupService(_mockCache.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expectedDocument = new PremisesActivityTypeDocument
        {
            IdentifierId = "test-id",
            Code = "MARP",
            Name = "Market on Paved Ground",
            IsActive = true,
            PriorityOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.PremisesActivityTypes).Returns(new[] { expectedDocument });

        // Act
        var result = await _sut.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().Be(expectedDocument);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockCache.Setup(c => c.PremisesActivityTypes).Returns(Array.Empty<PremisesActivityTypeDocument>());

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingPremiseActivityType()
    {
        // Arrange
        var doc = new PremisesActivityTypeDocument
        {
            IdentifierId = "MARP",
            Code = "MARP",
            Name = "Market on Paved Ground",
            IsActive = true,
            PriorityOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.PremisesActivityTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("MARP", CancellationToken.None);

        // Assert
        result.premiseActivityTypeId.Should().Be("MARP");
        result.premiseActivityTypeName.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithName_ReturnsMatchingPremiseActivityType()
    {
        // Arrange
        var doc = new PremisesActivityTypeDocument
        {
            IdentifierId = "CC",
            Code = "CC",
            Name = "Collection Centre",
            IsActive = true,
            PriorityOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.PremisesActivityTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("Collection Centre", CancellationToken.None);

        // Assert
        result.premiseActivityTypeId.Should().Be("CC");
        result.premiseActivityTypeName.Should().Be("Collection Centre");
    }

    [Fact]
    public async Task FindAsync_WhenNotFound_ReturnsNullTuple()
    {
        // Arrange
        _mockCache.Setup(c => c.PremisesActivityTypes).Returns(Array.Empty<PremisesActivityTypeDocument>());

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.premiseActivityTypeId.Should().BeNull();
        result.premiseActivityTypeName.Should().BeNull();
    }
}