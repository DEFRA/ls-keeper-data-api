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

public class SpeciesRepositoryTests
{
    private readonly IOptions<MongoConfig> _mongoConfig;
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IAsyncCursor<SpeciesListDocument>> _asyncCursorMock = new();
    private readonly Mock<IMongoCollection<SpeciesListDocument>> _mongoCollectionMock = new();

    private readonly SpeciesRepository _sut;

    public SpeciesRepositoryTests()
    {
        _mongoConfig = Options.Create(new MongoConfig { DatabaseName = "TestDatabase" });

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<SpeciesListDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
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
                It.IsAny<FilterDefinition<SpeciesListDocument>>(),
                It.IsAny<FindOptions<SpeciesListDocument, SpeciesListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session)
            .Returns(_clientSessionHandleMock.Object);

        _sut = new SpeciesRepository(_mongoConfig, _mongoClientMock.Object, _unitOfWorkMock.Object);

        typeof(GenericRepository<SpeciesListDocument>)
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_sut, _mongoCollectionMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_ReturnsMatchingSpecies()
    {
        // Arrange
        var bovineId = Guid.NewGuid().ToString();
        var porcineId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = porcineId, Code = "POR", Name = "Porcine", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(bovineId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(bovineId);
        result.Code.Should().Be("BOV");
        result.Name.Should().Be("Bovine");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdentifierId_IsCaseInsensitive()
    {
        // Arrange
        var bovineId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(bovineId.ToUpper(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(bovineId);
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
    public async Task GetByIdAsync_WhenSpeciesNotFound_ReturnsNull()
    {
        // Arrange
        var bovineId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithValidCode_ReturnsSpeciesIdAndName()
    {
        // Arrange
        var bovineId = Guid.NewGuid().ToString();
        var porcineId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = porcineId, Code = "POR", Name = "Porcine", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("BOV", CancellationToken.None);

        // Assert
        result.speciesId.Should().Be(bovineId);
        result.speciesName.Should().Be("Bovine");
    }

    [Fact]
    public async Task FindAsync_WhenCodeNotFoundButNameMatches_ReturnsSpeciesIdAndName()
    {
        // Arrange
        var bovineId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Bovine", CancellationToken.None);

        // Assert
        result.speciesId.Should().Be(bovineId);
        result.speciesName.Should().Be("Bovine");
    }

    [Fact]
    public async Task FindAsync_WhenMatchingByCodeOrName_IsCaseInsensitive()
    {
        // Arrange
        var bovineId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var resultByCode = await _sut.FindAsync("bov", CancellationToken.None);
        var resultByName = await _sut.FindAsync("bovine", CancellationToken.None);

        // Assert
        resultByCode.speciesId.Should().Be(bovineId);
        resultByName.speciesId.Should().Be(bovineId);
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
        result.speciesId.Should().BeNull();
        result.speciesName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenSpeciesNotFound_ReturnsNulls()
    {
        // Arrange
        var bovineId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Bovine", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Unknown", CancellationToken.None);

        // Assert
        result.speciesId.Should().BeNull();
        result.speciesName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchExists_PrefersCodeOverName()
    {
        // Arrange
        var bovineId = Guid.NewGuid().ToString();
        var otherId = Guid.NewGuid().ToString();

        var species = new List<SpeciesDocument>
        {
            new() { IdentifierId = bovineId, Code = "BOV", Name = "Cattle", IsActive = true, SortOrder = 10, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow },
            new() { IdentifierId = otherId, Code = "OTHER", Name = "BOV", IsActive = true, SortOrder = 20, EffectiveStartDate = DateTime.UtcNow, CreatedBy = "System", CreatedDate = DateTime.UtcNow }
        };

        var listDocument = new SpeciesListDocument
        {
            Id = "all-species",
            Species = species
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("BOV", CancellationToken.None);

        // Assert
        result.speciesId.Should().Be(bovineId);
        result.speciesName.Should().Be("Cattle");
    }
}