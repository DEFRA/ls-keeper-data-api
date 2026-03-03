using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class PremiseTypeLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly PremiseTypeLookupService _sut;

    public PremiseTypeLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.PremisesTypes).Returns(Array.Empty<PremisesTypeDocument>());
        _sut = new PremiseTypeLookupService(_mockCache.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expectedDocument = new PremisesTypeDocument
        {
            IdentifierId = "test-id",
            Code = "AC",
            Name = "Assembly Centre",
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.PremisesTypes).Returns(new[] { expectedDocument });

        // Act
        var result = await _sut.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().Be(expectedDocument);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockCache.Setup(c => c.PremisesTypes).Returns(Array.Empty<PremisesTypeDocument>());

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingPremiseType()
    {
        // Arrange
        var doc = new PremisesTypeDocument
        {
            IdentifierId = "AC",
            Code = "AC",
            Name = "Assembly Centre",
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.PremisesTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("AC", CancellationToken.None);

        // Assert
        result.premiseTypeId.Should().Be("AC");
        result.premiseTypeName.Should().Be("Assembly Centre");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithName_ReturnsMatchingPremiseType()
    {
        // Arrange
        var doc = new PremisesTypeDocument
        {
            IdentifierId = "MA",
            Code = "MA",
            Name = "Market",
            IsActive = true,
            SortOrder = 0,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.PremisesTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("Market", CancellationToken.None);

        // Assert
        result.premiseTypeId.Should().Be("MA");
        result.premiseTypeName.Should().Be("Market");
    }

    [Fact]
    public async Task FindAsync_WhenNotFound_ReturnsNullTuple()
    {
        // Arrange
        _mockCache.Setup(c => c.PremisesTypes).Returns(Array.Empty<PremisesTypeDocument>());

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.premiseTypeId.Should().BeNull();
        result.premiseTypeName.Should().BeNull();
    }
}