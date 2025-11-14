using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class CountryIdentifierLookupServiceTests
{
    private readonly Mock<ICountryRepository> _mockCountryRepository;
    private readonly CountryIdentifierLookupService _sut;

    public CountryIdentifierLookupServiceTests()
    {
        _mockCountryRepository = new Mock<ICountryRepository>();
        _sut = new CountryIdentifierLookupService(_mockCountryRepository.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCountryFound_ReturnsCountry()
    {
        // Arrange
        var countryId = Guid.NewGuid().ToString();
        var expectedCountry = new CountryDocument
        {
            IdentifierId = countryId,
            Code = "GB",
            Name = "United Kingdom",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };

        _mockCountryRepository
            .Setup(x => x.GetByIdAsync(countryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCountry);

        // Act
        var result = await _sut.GetByIdAsync(countryId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(expectedCountry.Code);
        result.Name.Should().Be(expectedCountry.Name);
        _mockCountryRepository.Verify(x => x.GetByIdAsync(countryId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCountryNotFound_ReturnsNull()
    {
        // Arrange
        var countryId = Guid.NewGuid().ToString();

        _mockCountryRepository
            .Setup(x => x.GetByIdAsync(countryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryDocument?)null);

        // Act
        var result = await _sut.GetByIdAsync(countryId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockCountryRepository.Verify(x => x.GetByIdAsync(countryId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WhenCountryFound_ReturnsCountryIdAndName()
    {
        // Arrange
        var lookupValue = "GB";
        var expectedResult = ("GB", "United Kingdom");

        _mockCountryRepository
            .Setup(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        // Assert
        result.countryId.Should().Be(expectedResult.Item1);
        result.countryName.Should().Be(expectedResult.Item2);
        _mockCountryRepository.Verify(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WhenCountryNotFound_ReturnsNulls()
    {
        // Arrange
        var lookupValue = "Unknown";
        var expectedResult = ((string?)null, (string?)null);

        _mockCountryRepository
            .Setup(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        // Assert
        result.countryId.Should().BeNull();
        result.countryName.Should().BeNull();
        _mockCountryRepository.Verify(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()), Times.Once);
    }
}