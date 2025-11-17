using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class PremiseActivityTypeLookupServiceTests
{
    private readonly Mock<IPremisesActivityTypeRepository> _mockRepository;
    private readonly PremiseActivityTypeLookupService _sut;

    public PremiseActivityTypeLookupServiceTests()
    {
        _mockRepository = new Mock<IPremisesActivityTypeRepository>();
        _sut = new PremiseActivityTypeLookupService(_mockRepository.Object);
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
            .ReturnsAsync((PremisesActivityTypeDocument?)null);

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingPremiseActivityType()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.FindAsync("MARP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("MARP", "Market on Paved Ground"));

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
        _mockRepository
            .Setup(r => r.FindAsync("Collection Centre", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("CC", "Collection Centre"));

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
        _mockRepository
            .Setup(x => x.FindAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.premiseActivityTypeId.Should().BeNull();
        result.premiseActivityTypeName.Should().BeNull();
    }
}