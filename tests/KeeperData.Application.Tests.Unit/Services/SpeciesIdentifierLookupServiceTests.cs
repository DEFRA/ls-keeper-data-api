using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class SpeciesIdentifierLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly SpeciesTypeLookupService _sut;

    public SpeciesIdentifierLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.Species).Returns(Array.Empty<SpeciesDocument>());
        _sut = new SpeciesTypeLookupService(_mockCache.Object);
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

        _mockCache.Setup(c => c.Species).Returns(new[] { expectedSpecies });

        // Act
        var result = await _sut.GetByIdAsync(speciesId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(expectedSpecies.Code);
        result.Name.Should().Be(expectedSpecies.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSpeciesNotFound_ReturnsNull()
    {
        // Arrange
        var speciesId = Guid.NewGuid().ToString();
        _mockCache.Setup(c => c.Species).Returns(Array.Empty<SpeciesDocument>());

        // Act
        var result = await _sut.GetByIdAsync(speciesId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenSpeciesFound_ReturnsSpeciesIdAndName()
    {
        // Arrange
        var species = new SpeciesDocument
        {
            IdentifierId = "BOV",
            Code = "BOV",
            Name = "Bovine",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.Species).Returns(new[] { species });

        // Act
        var result = await _sut.FindAsync("BOV", CancellationToken.None);

        // Assert
        result.speciesTypeId.Should().Be("BOV");
        result.speciesTypeName.Should().Be("Bovine");
    }

    [Fact]
    public async Task FindAsync_WhenSpeciesNotFound_ReturnsNulls()
    {
        // Arrange
        _mockCache.Setup(c => c.Species).Returns(Array.Empty<SpeciesDocument>());

        // Act
        var result = await _sut.FindAsync("Unknown", CancellationToken.None);

        // Assert
        result.speciesTypeId.Should().BeNull();
        result.speciesTypeName.Should().BeNull();
    }
}