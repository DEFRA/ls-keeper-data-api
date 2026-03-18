using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class CountryIdentifierLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly CountryIdentifierLookupService _sut;

    public CountryIdentifierLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.Countries).Returns(Array.Empty<CountryDocument>());
        _sut = new CountryIdentifierLookupService(_mockCache.Object);
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

        _mockCache.Setup(c => c.Countries).Returns(new[] { expectedCountry });

        // Act
        var result = await _sut.GetByIdAsync(countryId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(expectedCountry.Code);
        result.Name.Should().Be(expectedCountry.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCountryNotFound_ReturnsNull()
    {
        // Arrange
        var countryId = Guid.NewGuid().ToString();
        _mockCache.Setup(c => c.Countries).Returns(Array.Empty<CountryDocument>());

        // Act
        var result = await _sut.GetByIdAsync(countryId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCountryFound_ReturnsCountryIdAndName()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        var country = new CountryDocument
        {
            IdentifierId = expectedId,
            Code = "GB",
            Name = "United Kingdom",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.Countries).Returns(new[] { country });

        // Act
        var (countryId, countryCode, countryName) = await _sut.FindAsync("GB", CancellationToken.None);

        // Assert
        countryId.Should().Be(expectedId);
        countryCode.Should().Be("GB");
        countryName.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task FindAsync_WhenCountryNotFound_ReturnsNulls()
    {
        // Arrange
        _mockCache.Setup(c => c.Countries).Returns(Array.Empty<CountryDocument>());

        // Act
        var (countryId, countryCode, countryName) = await _sut.FindAsync("Unknown", CancellationToken.None);

        // Assert
        countryId.Should().BeNull();
        countryName.Should().BeNull();
    }

    [Theory]
    [InlineData("GB", "ENGLAND", "GB-ENG")]
    [InlineData("GB", "WALES", "GB-WLS")]
    [InlineData("GB", "SCOTLAND", "GB-SCT")]
    [InlineData("GB", "NORTHERN IRELAND", "GB-NIR")]
    public async Task FindAsync_WithUkInternalCode_MapsToSubDivision(string countryCode, string ukInternalCode, string expectedSearchKey)
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        var country = new CountryDocument
        {
            IdentifierId = expectedId,
            Code = expectedSearchKey,
            Name = "Country Name",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.Countries).Returns(new[] { country });

        // Act
        var result = await _sut.FindAsync(countryCode, ukInternalCode, CancellationToken.None);

        // Assert
        result.countryId.Should().Be(expectedId);
        result.countryCode.Should().Be(expectedSearchKey);
        result.countryName.Should().Be("Country Name");
    }

    [Fact]
    public async Task FindAsync_WithGbAndUnknownInternalCode_FallsBackToGb()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        var country = new CountryDocument
        {
            IdentifierId = expectedId,
            Code = "GB",
            Name = "United Kingdom",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.Countries).Returns(new[] { country });

        // Act
        var result = await _sut.FindAsync("GB", "UNKNOWN", CancellationToken.None);

        // Assert
        result.countryId.Should().Be(expectedId);
        result.countryCode.Should().Be("GB");
        result.countryName.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task FindAsync_WithNonGbCountry_IgnoresInternalCode()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        var country = new CountryDocument
        {
            IdentifierId = expectedId,
            Code = "FR",
            Name = "France",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.Countries).Returns(new[] { country });

        // Act
        var result = await _sut.FindAsync("FR", "ENGLAND", CancellationToken.None);

        // Assert
        result.countryId.Should().Be(expectedId);
        result.countryCode.Should().Be("FR");
        result.countryName.Should().Be("France");
    }

    [Fact]
    public async Task FindAsync_WithNullCountryCode_ReturnsNull()
    {
        // Act
        var (countryId, countryCode, countryName) = await _sut.FindAsync(null, "ENGLAND", CancellationToken.None);

        // Assert
        countryId.Should().BeNull();
    }
}