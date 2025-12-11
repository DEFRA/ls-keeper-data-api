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

    [Theory]
    [InlineData("GB", "ENGLAND", "GB-ENG")]
    [InlineData("GB", "WALES", "GB-WLS")]
    [InlineData("GB", "SCOTLAND", "GB-SCT")]
    [InlineData("GB", "NORTHERN IRELAND", "GB-NIR")]
    public async Task FindAsync_WithUkInternalCode_MapsToSubDivision(string countryCode, string ukInternalCode, string expectedSearchKey)
    {
        // Arrange
        var expectedResult = (expectedSearchKey, "Sub Division Name");

        _mockCountryRepository
            .Setup(x => x.FindAsync(expectedSearchKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(countryCode, ukInternalCode, CancellationToken.None);

        // Assert
        _mockCountryRepository.Verify(x => x.FindAsync(expectedSearchKey, It.IsAny<CancellationToken>()), Times.Once);
        result.countryId.Should().Be(expectedSearchKey);
    }

    [Fact]
    public async Task FindAsync_WithGbAndUnknownInternalCode_FallsBackToGb()
    {
        // Arrange
        var countryCode = "GB";
        var ukInternalCode = "UNKNOWN";
        var expectedResult = ("GB", "United Kingdom");

        _mockCountryRepository
            .Setup(x => x.FindAsync("GB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(countryCode, ukInternalCode, CancellationToken.None);

        // Assert
        _mockCountryRepository.Verify(x => x.FindAsync("GB", It.IsAny<CancellationToken>()), Times.Once);
        result.countryId.Should().Be("GB");
    }

    [Fact]
    public async Task FindAsync_WithNonGbCountry_IgnoresInternalCode()
    {
        // Arrange
        var countryCode = "FR";
        var ukInternalCode = "ENGLAND"; // Should be ignored
        var expectedResult = ("FR", "France");

        _mockCountryRepository
            .Setup(x => x.FindAsync("FR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(countryCode, ukInternalCode, CancellationToken.None);

        // Assert
        _mockCountryRepository.Verify(x => x.FindAsync("FR", It.IsAny<CancellationToken>()), Times.Once);
        result.countryId.Should().Be("FR");
    }

    [Fact]
    public async Task FindAsync_WithNullCountryCode_ReturnsNull()
    {
        // Act
        var result = await _sut.FindAsync(null, "ENGLAND", CancellationToken.None);

        // Assert
        result.countryId.Should().BeNull();
        _mockCountryRepository.Verify(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}