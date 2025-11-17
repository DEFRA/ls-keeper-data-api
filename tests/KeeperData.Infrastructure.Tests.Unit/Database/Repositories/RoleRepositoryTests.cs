using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class RoleRepositoryTests
{
    private readonly IOptions<MongoConfig> _mongoConfig;
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IAsyncCursor<RoleListDocument>> _asyncCursorMock = new();
    private readonly Mock<IMongoCollection<RoleListDocument>> _mongoCollectionMock = new();

    private readonly RoleRepository _sut;

    public RoleRepositoryTests()
    {
        _mongoConfig = Options.Create(new MongoConfig { DatabaseName = "TestDatabase" });

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<RoleListDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(_mongoCollectionMock.Object);

        _mongoClientMock
            .Setup(client => client.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(_mongoDatabaseMock.Object);

        _asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        _mongoCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<RoleListDocument>>(),
                It.IsAny<FindOptions<RoleListDocument, RoleListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session)
            .Returns(_clientSessionHandleMock.Object);

        _sut = new RoleRepository(_mongoConfig, _mongoClientMock.Object, _unitOfWorkMock.Object);

        typeof(GenericRepository<RoleListDocument>)
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_sut, _mongoCollectionMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_ReturnsMatchingRole()
    {
        // Arrange
        var keeperId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();

        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = keeperId, Code = "LIVESTOCKKEEPER", Name = "Livestock Keeper", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = ownerId, Code = "OWNER", Name = "Owner", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new RoleListDocument
        {
            Id = "all-roles",
            Roles = roles
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(keeperId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(keeperId);
        result.Code.Should().Be("LIVESTOCKKEEPER");
        result.Name.Should().Be("Livestock Keeper");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_IsCaseInsensitive()
    {
        // Arrange
        var keeperId = Guid.NewGuid().ToString();

        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = keeperId, Code = "LIVESTOCKKEEPER", Name = "Livestock Keeper", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new RoleListDocument
        {
            Id = "all-roles",
            Roles = roles
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(keeperId.ToUpper(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(keeperId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WhenCalledWithNullOrEmpty_ReturnsNull(string? id)
    {
        // Act
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoleNotFound_ReturnsNull()
    {
        // Arrange
        var keeperId = Guid.NewGuid().ToString();

        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = keeperId, Code = "LIVESTOCKKEEPER", Name = "Livestock Keeper", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new RoleListDocument
        {
            Id = "all-roles",
            Roles = roles
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidCode_ReturnsRoleIdAndName()
    {
        // Arrange
        var keeperId = Guid.NewGuid().ToString();
        var ownerId = Guid.NewGuid().ToString();

        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = keeperId, Code = "LIVESTOCKKEEPER", Name = "Livestock Keeper", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = ownerId, Code = "OWNER", Name = "Owner", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new RoleListDocument
        {
            Id = "all-roles",
            Roles = roles
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("LIVESTOCKKEEPER", CancellationToken.None);

        // Assert
        result.roleId.Should().Be("LIVESTOCKKEEPER");
        result.roleName.Should().Be("Livestock Keeper");
    }

    [Fact]
    public async Task FindAsync_WhenCodeNotFoundButNameMatches_ReturnsRoleIdAndName()
    {
        // Arrange
        var keeperId = Guid.NewGuid().ToString();

        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = keeperId, Code = "LIVESTOCKKEEPER", Name = "Livestock Keeper", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new RoleListDocument
        {
            Id = "all-roles",
            Roles = roles
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Livestock Keeper", CancellationToken.None);

        // Assert
        result.roleId.Should().Be("LIVESTOCKKEEPER");
        result.roleName.Should().Be("Livestock Keeper");
    }

    [Fact]
    public async Task FindAsync_WhenMatchingByCodeOrName_IsCaseInsensitive()
    {
        // Arrange
        var keeperId = Guid.NewGuid().ToString();

        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = keeperId, Code = "LIVESTOCKKEEPER", Name = "Livestock Keeper", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new RoleListDocument
        {
            Id = "all-roles",
            Roles = roles
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var resultByCode = await _sut.FindAsync("livestockkeeper", CancellationToken.None);
        var resultByName = await _sut.FindAsync("livestock keeper", CancellationToken.None);

        // Assert
        resultByCode.roleId.Should().Be("LIVESTOCKKEEPER");
        resultByName.roleId.Should().Be("LIVESTOCKKEEPER");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_WhenCalledWithNullOrEmpty_ReturnsNulls(string? lookupValue)
    {
        // Act
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        // Assert
        result.roleId.Should().BeNull();
        result.roleName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenRoleNotFound_ReturnsNulls()
    {
        // Arrange
        var keeperId = Guid.NewGuid().ToString();

        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = keeperId, Code = "LIVESTOCKKEEPER", Name = "Livestock Keeper", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new RoleListDocument
        {
            Id = "all-roles",
            Roles = roles
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Unknown", CancellationToken.None);

        // Assert
        result.roleId.Should().BeNull();
        result.roleName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchExists_PrefersCodeOverName()
    {
        // Arrange
        var keeperId = Guid.NewGuid().ToString();
        var otherId = Guid.NewGuid().ToString();

        var roles = new List<RoleDocument>
        {
            new() { IdentifierId = keeperId, Code = "KEEPER", Name = "Keeper", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = otherId, Code = "OTHER", Name = "KEEPER", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new RoleListDocument
        {
            Id = "all-roles",
            Roles = roles
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("KEEPER", CancellationToken.None);

        // Assert
        result.roleId.Should().Be("KEEPER");
        result.roleName.Should().Be("Keeper");
    }
}