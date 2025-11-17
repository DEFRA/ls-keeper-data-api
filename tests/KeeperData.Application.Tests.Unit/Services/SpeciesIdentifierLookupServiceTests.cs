using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class SpeciesIdentifierLookupServiceTests
{
    private readonly Mock<ISpeciesRepository> _mockSpeciesRepository;
    private readonly SpeciesTypeLookupService _sut;

    public SpeciesIdentifierLookupServiceTests()
    {
        _mockSpeciesRepository = new Mock<ISpeciesRepository>();
        _sut = new SpeciesTypeLookupService(_mockSpeciesRepository.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSpeciesFound_ReturnsSpecies()
    {
        // Arrange
        var speciesId = Guid.NewGuid().ToString();
        var expectedSpecies = new SpeciesDocument
        {
            IdentifierId = speciesId,
            Code = "BOV",
            Name = "Bovine",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };

        _mockSpeciesRepository
            .Setup(x => x.GetByIdAsync(speciesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSpecies);

        // Act
        var result = await _sut.GetByIdAsync(speciesId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(expectedSpecies.Code);
        result.Name.Should().Be(expectedSpecies.Name);
        _mockSpeciesRepository.Verify(x => x.GetByIdAsync(speciesId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSpeciesNotFound_ReturnsNull()
    {
        // Arrange
        var speciesId = Guid.NewGuid().ToString();

        _mockSpeciesRepository
            .Setup(x => x.GetByIdAsync(speciesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpeciesDocument?)null);

        // Act
        var result = await _sut.GetByIdAsync(speciesId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockSpeciesRepository.Verify(x => x.GetByIdAsync(speciesId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WhenSpeciesFound_ReturnsSpeciesIdAndName()
    {
        // Arrange
        var lookupValue = "BOV";
        var expectedResult = ("BOV", "Bovine");

        _mockSpeciesRepository
            .Setup(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        // Assert
        result.speciesTypeId.Should().Be(expectedResult.Item1);
        result.speciesTypeName.Should().Be(expectedResult.Item2);
        _mockSpeciesRepository.Verify(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WhenSpeciesNotFound_ReturnsNulls()
    {
        // Arrange
        var lookupValue = "Unknown";
        var expectedResult = ((string?)null, (string?)null);

        _mockSpeciesRepository
            .Setup(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        // Assert
        result.speciesTypeId.Should().BeNull();
        result.speciesTypeName.Should().BeNull();
        _mockSpeciesRepository.Verify(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()), Times.Once);
    }
}