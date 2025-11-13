using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class SiteIdentifierTypeLookupServiceTests
{
    private readonly Mock<ISiteIdentifierTypeRepository> _mockRepository;
    private readonly SiteIdentifierTypeLookupService _sut;

    public SiteIdentifierTypeLookupServiceTests()
    {
        _mockRepository = new Mock<ISiteIdentifierTypeRepository>();
        _sut = new SiteIdentifierTypeLookupService(_mockRepository.Object);
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
            .ReturnsAsync((SiteIdentifierTypeDocument?)null);

        // Act
        var result = await _sut.GetByIdAsync("non-existent", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidLookupValue_ReturnsMatchingSiteIdentifierType()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.FindAsync("CPHN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("CPHN", "CPH Number"));

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
        _mockRepository
            .Setup(r => r.FindAsync("Port Number", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("PRTN", "Port Number"));

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
        _mockRepository
            .Setup(x => x.FindAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(((string?)null, (string?)null));

        // Act
        var result = await _sut.FindAsync("NONEXISTENT", CancellationToken.None);

        // Assert
        result.siteIdentifierId.Should().BeNull();
        result.siteIdentifierName.Should().BeNull();
    }
}