using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class PremiseTypeLookupServiceTests
{
    private readonly Mock<IPremisesTypeRepository> _mockRepository;
    private readonly PremiseTypeLookupService _sut;

    public PremiseTypeLookupServiceTests()
    {
        _mockRepository = new Mock<IPremisesTypeRepository>();
        _sut = new PremiseTypeLookupService(_mockRepository.Object);
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
            .ReturnsAsync((PremisesTypeDocument?)null);

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingPremiseType()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.FindAsync("AC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("AC", "Assembly Centre"));

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
        _mockRepository
            .Setup(r => r.FindAsync("Market", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("MA", "Market"));

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
        _mockRepository
            .Setup(x => x.FindAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.premiseTypeId.Should().BeNull();
        result.premiseTypeName.Should().BeNull();
    }
}