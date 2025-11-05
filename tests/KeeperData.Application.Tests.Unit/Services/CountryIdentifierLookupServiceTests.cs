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
    public async Task GetByIdAsync_ReturnsCountry_WhenFound()
    {
        // Arrange
        var countryId = "GB";
        var expectedCountry = new CountryDocument
        {
            IdentifierId = countryId,
            Code = "GB",
            Name = "United Kingdom",
            IsActive = true
        };

        _mockCountryRepository
            .Setup(x => x.GetByIdAsync(countryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCountry);

        // Act
        var result = await _sut.GetByIdAsync(countryId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCountry.Code, result.Code);
        Assert.Equal(expectedCountry.Name, result.Name);
        _mockCountryRepository.Verify(x => x.GetByIdAsync(countryId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var countryId = "XX";

        _mockCountryRepository
            .Setup(x => x.GetByIdAsync(countryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryDocument?)null);

        // Act
        var result = await _sut.GetByIdAsync(countryId, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockCountryRepository.Verify(x => x.GetByIdAsync(countryId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ReturnsCountryIdAndName_WhenFound()
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
        Assert.Equal(expectedResult.Item1, result.countryId);
        Assert.Equal(expectedResult.Item2, result.countryName);
        _mockCountryRepository.Verify(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ReturnsNulls_WhenNotFound()
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
        Assert.Null(result.countryId);
        Assert.Null(result.countryName);
        _mockCountryRepository.Verify(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()), Times.Once);
    }
}