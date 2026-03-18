using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class SiteIdentifierTypeLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly SiteIdentifierTypeLookupService _sut;

    public SiteIdentifierTypeLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.SiteIdentifierTypes).Returns(Array.Empty<SiteIdentifierTypeDocument>());
        _sut = new SiteIdentifierTypeLookupService(_mockCache.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expectedDocument = new SiteIdentifierTypeDocument
        {
            IdentifierId = "test-id",
            Code = "CPHN",
            Name = "CPH Number",
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteIdentifierTypes).Returns(new[] { expectedDocument });

        // Act
        var result = await _sut.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().Be(expectedDocument);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockCache.Setup(c => c.SiteIdentifierTypes).Returns(Array.Empty<SiteIdentifierTypeDocument>());

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingSiteIdentifierType()
    {
        // Arrange
        var doc = new SiteIdentifierTypeDocument
        {
            IdentifierId = "CPHN",
            Code = "CPHN",
            Name = "CPH Number",
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteIdentifierTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("CPHN", CancellationToken.None);

        // Assert
        result.siteIdentifierId.Should().Be("CPHN");
        result.siteIdentifierName.Should().Be("CPH Number");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithName_ReturnsMatchingSiteIdentifierType()
    {
        // Arrange
        var doc = new SiteIdentifierTypeDocument
        {
            IdentifierId = "PRTN",
            Code = "PRTN",
            Name = "Port Number",
            IsActive = true,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.SiteIdentifierTypes).Returns(new[] { doc });

        // Act
        var result = await _sut.FindAsync("Port Number", CancellationToken.None);

        // Assert
        result.siteIdentifierId.Should().Be("PRTN");
        result.siteIdentifierName.Should().Be("Port Number");
    }

    [Fact]
    public async Task FindAsync_WhenNotFound_ReturnsNullTuple()
    {
        // Arrange
        _mockCache.Setup(c => c.SiteIdentifierTypes).Returns(Array.Empty<SiteIdentifierTypeDocument>());

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.siteIdentifierId.Should().BeNull();
        result.siteIdentifierName.Should().BeNull();
    }
}