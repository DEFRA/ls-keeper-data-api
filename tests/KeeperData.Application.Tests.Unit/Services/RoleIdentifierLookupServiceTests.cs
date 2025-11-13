using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class RoleIdentifierLookupServiceTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly RoleTypeLookupService _sut;

    public RoleIdentifierLookupServiceTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _sut = new RoleTypeLookupService(_mockRoleRepository.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoleFound_ReturnsRole()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        var expectedRole = new RoleDocument
        {
            IdentifierId = roleId,
            Code = "LIVESTOCKKEEPER",
            Name = "Livestock Keeper",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };

        _mockRoleRepository
            .Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRole);

        // Act
        var result = await _sut.GetByIdAsync(roleId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(expectedRole.Code);
        result.Name.Should().Be(expectedRole.Name);
        _mockRoleRepository.Verify(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoleNotFound_ReturnsNull()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();

        _mockRoleRepository
            .Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleDocument?)null);

        // Act
        var result = await _sut.GetByIdAsync(roleId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockRoleRepository.Verify(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WhenRoleFound_ReturnsRoleIdAndName()
    {
        // Arrange
        var lookupValue = "LIVESTOCKKEEPER";
        var expectedResult = ("LIVESTOCKKEEPER", "Livestock Keeper");

        _mockRoleRepository
            .Setup(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        // Assert
        result.roleTypeId.Should().Be(expectedResult.Item1);
        result.roleTypeName.Should().Be(expectedResult.Item2);
        _mockRoleRepository.Verify(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WhenRoleNotFound_ReturnsNulls()
    {
        // Arrange
        var lookupValue = "Unknown";
        var expectedResult = ((string?)null, (string?)null);

        _mockRoleRepository
            .Setup(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        // Assert
        result.roleTypeId.Should().BeNull();
        result.roleTypeName.Should().BeNull();
        _mockRoleRepository.Verify(x => x.FindAsync(lookupValue, It.IsAny<CancellationToken>()), Times.Once);
    }
}