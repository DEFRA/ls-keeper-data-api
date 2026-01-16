using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Reference;
using KeeperData.Infrastructure.Database.Repositories;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class RoleRepositoryTests
{
    private readonly ReferenceRepositoryTestFixture<RoleRepository, RoleListDocument, RoleDocument> _fixture;
    private readonly RoleRepository _sut;

    public RoleRepositoryTests()
    {
        _fixture = new ReferenceRepositoryTestFixture<RoleRepository, RoleListDocument, RoleDocument>();
        _sut = _fixture.CreateSut((config, client, unitOfWork) => new RoleRepository(config, client, unitOfWork));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_ReturnsMatchingRole()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(keeperId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(keeperId);
        result.Code.Should().Be("LIVESTOCKKEEPER");
        result.Name.Should().Be("Livestock Keeper");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_IsCaseInsensitive()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.GetByIdAsync(keeperId.ToUpper(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(keeperId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WhenCalledWithNullOrEmpty_ReturnsNull(string? id)
    {
        var result = await _sut.GetByIdAsync(id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoleNotFound_ReturnsNull()
    {
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

        _fixture.SetUpDocuments(listDocument);
        var nonExistentId = Guid.NewGuid().ToString();

        var result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidCode_ReturnsRoleIdAndName()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("LIVESTOCKKEEPER", CancellationToken.None);

        result.roleId.Should().Be(keeperId);
        result.roleName.Should().Be("Livestock Keeper");
    }

    [Fact]
    public async Task FindAsync_WhenCodeNotFoundButNameMatches_ReturnsRoleIdAndName()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Livestock Keeper", CancellationToken.None);

        result.roleId.Should().Be(keeperId);
        result.roleName.Should().Be("Livestock Keeper");
    }

    [Fact]
    public async Task FindAsync_WhenMatchingByCodeOrName_IsCaseInsensitive()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var resultByCode = await _sut.FindAsync("livestockkeeper", CancellationToken.None);
        var resultByName = await _sut.FindAsync("livestock keeper", CancellationToken.None);

        resultByCode.roleId.Should().Be(keeperId);
        resultByName.roleId.Should().Be(keeperId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_WhenCalledWithNullOrEmpty_ReturnsNulls(string? lookupValue)
    {
        var result = await _sut.FindAsync(lookupValue, CancellationToken.None);

        result.roleId.Should().BeNull();
        result.roleName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenRoleNotFound_ReturnsNulls()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("Unknown", CancellationToken.None);

        result.roleId.Should().BeNull();
        result.roleName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchExists_PrefersCodeOverName()
    {
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

        _fixture.SetUpDocuments(listDocument);

        var result = await _sut.FindAsync("KEEPER", CancellationToken.None);

        result.roleId.Should().Be(keeperId);
        result.roleName.Should().Be("Keeper");
    }
}