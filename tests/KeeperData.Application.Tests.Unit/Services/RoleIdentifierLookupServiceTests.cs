using FluentAssertions;
using KeeperData.Application.Services;
using KeeperData.Core.Documents;
using KeeperData.Core.Services;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services;

public class RoleIdentifierLookupServiceTests
{
    private readonly Mock<IReferenceDataCache> _mockCache;
    private readonly RoleTypeLookupService _sut;

    public RoleIdentifierLookupServiceTests()
    {
        _mockCache = new Mock<IReferenceDataCache>();
        _mockCache.Setup(c => c.Roles).Returns(Array.Empty<RoleDocument>());
        _sut = new RoleTypeLookupService(_mockCache.Object);
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

        _mockCache.Setup(c => c.Roles).Returns(new[] { expectedRole });

        // Act
        var result = await _sut.GetByIdAsync(roleId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(expectedRole.Code);
        result.Name.Should().Be(expectedRole.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoleNotFound_ReturnsNull()
    {
        // Arrange
        var roleId = Guid.NewGuid().ToString();
        _mockCache.Setup(c => c.Roles).Returns(Array.Empty<RoleDocument>());

        // Act
        var result = await _sut.GetByIdAsync(roleId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenRoleFound_ReturnsRoleIdAndName()
    {
        // Arrange
        var expectedId = Guid.NewGuid().ToString();
        var role = new RoleDocument
        {
            IdentifierId = expectedId,
            Code = "LIVESTOCKKEEPER",
            Name = "Livestock Keeper",
            IsActive = true,
            SortOrder = 10,
            EffectiveStartDate = DateTime.UtcNow,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };
        _mockCache.Setup(c => c.Roles).Returns(new[] { role });

        // Act
        var (roleTypeId, roleTypeCode, roleTypeName) = await _sut.FindAsync("LIVESTOCKKEEPER", CancellationToken.None);

        // Assert
        roleTypeId.Should().Be(expectedId);
        roleTypeCode.Should().Be("LIVESTOCKKEEPER");
        roleTypeName.Should().Be("Livestock Keeper");
    }

    [Fact]
    public async Task FindAsync_WhenRoleNotFound_ReturnsNulls()
    {
        // Arrange
        _mockCache.Setup(c => c.Roles).Returns(Array.Empty<RoleDocument>());

        // Act
        var (roleTypeId, roleTypeCode, roleTypeName) = await _sut.FindAsync("Unknown", CancellationToken.None);

        // Assert
        roleTypeId.Should().BeNull();
        roleTypeName.Should().BeNull();
    }
}