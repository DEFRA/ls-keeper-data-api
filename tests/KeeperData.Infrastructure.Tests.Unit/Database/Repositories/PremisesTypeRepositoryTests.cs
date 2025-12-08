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

public class PremisesTypeRepositoryTests
{
    private readonly IOptions<MongoConfig> _mongoConfig;
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IAsyncCursor<PremisesTypeListDocument>> _asyncCursorMock = new();
    private readonly Mock<IMongoCollection<PremisesTypeListDocument>> _mongoCollectionMock = new();

    private readonly PremisesTypeRepository _sut;

    public PremisesTypeRepositoryTests()
    {
        _mongoConfig = Options.Create(new MongoConfig { DatabaseName = "TestDatabase" });

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<PremisesTypeListDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
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
                It.IsAny<FilterDefinition<PremisesTypeListDocument>>(),
                It.IsAny<FindOptions<PremisesTypeListDocument, PremisesTypeListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session)
            .Returns(_clientSessionHandleMock.Object);

        _sut = new PremisesTypeRepository(_mongoConfig, _mongoClientMock.Object, _unitOfWorkMock.Object);

        typeof(GenericRepository<PremisesTypeListDocument>)
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_sut, _mongoCollectionMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingPremisesType()
    {
        // Arrange
        var acId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a";
        var maId = "491cbd98-5bb7-46c3-abc7-30a232f65043";

        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = acId,
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = maId,
                Code = "MA",
                Name = "Market",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(acId);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(acId);
        result.Code.Should().Be("AC");
        result.Name.Should().Be("Assembly Centre");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingPremisesType()
    {
        // Arrange
        var acId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a";

        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = acId,
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync("1DBD0EA4-5F10-45A4-A0F6-E328A3074B6A");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("AC");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WhenCalledWithNullOrWhitespace_ReturnsNull(string? id)
    {
        // Arrange
        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
        // Arrange
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a",
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingCode_ReturnsCodeAndName()
    {
        // Arrange
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a",
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("AC");

        // Assert
        result.premiseTypeId.Should().Be("1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a");
        result.premiseTypeName.Should().Be("Assembly Centre");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingName_ReturnsCodeAndName()
    {
        // Arrange
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "491cbd98-5bb7-46c3-abc7-30a232f65043",
                Code = "MA",
                Name = "Market",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Market");

        // Assert
        result.premiseTypeId.Should().Be("491cbd98-5bb7-46c3-abc7-30a232f65043");
        result.premiseTypeName.Should().Be("Market");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDifferentCase_ReturnsCodeAndName()
    {
        // Arrange
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a",
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("ac");

        // Assert
        result.premiseTypeId.Should().Be("1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a");
        result.premiseTypeName.Should().Be("Assembly Centre");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAsync_WhenCalledWithNullOrWhitespace_ReturnsNullTuple(string? lookupValue)
    {
        // Arrange
        // Act
        var result = await _sut.FindAsync(lookupValue);

        // Assert
        result.premiseTypeId.Should().BeNull();
        result.premiseTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsNullTuple()
    {
        // Arrange
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "1dbd0ea4-5f10-45a4-a0f6-e328a3074b6a",
                Code = "AC",
                Name = "Assembly Centre",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("NONEXISTENT");

        // Assert
        result.premiseTypeId.Should().BeNull();
        result.premiseTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchesAndNameAlsoExists_PrioritizesCodeMatch()
    {
        // Arrange
        var premisesTypes = new List<PremisesTypeDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Code = "MA",
                Name = "Market",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = "id2",
                Code = "AC",
                Name = "MA",
                IsActive = true,
                SortOrder = 0,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesTypeListDocument
        {
            Id = "all-premisestypes",
            PremisesTypes = premisesTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("MA");

        // Assert
        result.premiseTypeId.Should().Be("id1");
        result.premiseTypeName.Should().Be("Market");
    }
}