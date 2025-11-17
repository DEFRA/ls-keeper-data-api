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

public class PremisesActivityTypeRepositoryTests
{
    private readonly Mock<IOptions<MongoConfig>> _configMock;
    private readonly Mock<IMongoClient> _mongoClientMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock;
    private readonly Mock<IMongoCollection<PremisesActivityTypeListDocument>> _collectionMock;
    private readonly Mock<IAsyncCursor<PremisesActivityTypeListDocument>> _asyncCursorMock;
    private readonly PremisesActivityTypeRepository _sut;

    public PremisesActivityTypeRepositoryTests()
    {
        _configMock = new Mock<IOptions<MongoConfig>>();
        _mongoClientMock = new Mock<IMongoClient>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _clientSessionHandleMock = new Mock<IClientSessionHandle>();
        _collectionMock = new Mock<IMongoCollection<PremisesActivityTypeListDocument>>();
        _asyncCursorMock = new Mock<IAsyncCursor<PremisesActivityTypeListDocument>>();

        _configMock.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "test" });

        var mockDatabase = new Mock<IMongoDatabase>();
        _mongoClientMock.Setup(c => c.GetDatabase(It.IsAny<string>(), null)).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.GetCollection<PremisesActivityTypeListDocument>(It.IsAny<string>(), null))
            .Returns(_collectionMock.Object);

        _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() =>
            {
                _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            });

        _collectionMock.Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<PremisesActivityTypeListDocument>>(),
                It.IsAny<FindOptions<PremisesActivityTypeListDocument, PremisesActivityTypeListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session).Returns(_clientSessionHandleMock.Object);

        _sut = new PremisesActivityTypeRepository(_configMock.Object, _mongoClientMock.Object, _unitOfWorkMock.Object);

        var collectionField = typeof(PremisesActivityTypeRepository).BaseType!.BaseType!
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
        collectionField!.SetValue(_sut, _collectionMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingPremisesActivityType()
    {
        // Arrange
        var marpId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab";
        var maruId = "89e6b48b-4aee-4a0f-9c0b-58e9aa6c3fb2";

        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = marpId,
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = maruId,
                Code = "MARU",
                Name = "Market on Unpaved Ground",
                IsActive = true,
                PriorityOrder = 20,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync(marpId);

        // Assert
        result.Should().NotBeNull();
        result!.IdentifierId.Should().Be(marpId);
        result.Code.Should().Be("MARP");
        result.Name.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidIdDifferentCase_ReturnsMatchingPremisesActivityType()
    {
        // Arrange
        var marpId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab";

        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = marpId,
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync("66D885C0-CE67-4CB5-8FD2-DD1F70A3C0AB");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("MARP");
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdNotFound_ReturnsNull()
    {
        // Arrange
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab",
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetByIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsNull_ReturnsNull()
    {
        // Arrange & Act
        var result = await _sut.GetByIdAsync(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsWhitespace_ReturnsNull()
    {
        // Arrange & Act
        var result = await _sut.GetByIdAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingCode_ReturnsCodeAndName()
    {
        // Arrange
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab",
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("MARP");

        // Assert
        result.premiseActivityTypeId.Should().Be("MARP");
        result.premiseActivityTypeName.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithMatchingName_ReturnsCodeAndName()
    {
        // Arrange
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "5e22d572-c98e-4892-98f7-c02c6eb37224",
                Code = "CC",
                Name = "Collection Centre",
                IsActive = true,
                PriorityOrder = 30,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("Collection Centre");

        // Assert
        result.premiseActivityTypeId.Should().Be("CC");
        result.premiseActivityTypeName.Should().Be("Collection Centre");
    }

    [Fact]
    public async Task FindAsync_WhenCalledWithDifferentCase_ReturnsCodeAndName()
    {
        // Arrange
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab",
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("marp");

        // Assert
        result.premiseActivityTypeId.Should().Be("MARP");
        result.premiseActivityTypeName.Should().Be("Market on Paved Ground");
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsNullTuple()
    {
        // Arrange
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "66d885c0-ce67-4cb5-8fd2-dd1f70a3c0ab",
                Code = "MARP",
                Name = "Market on Paved Ground",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("NONEXISTENT");

        // Assert
        result.premiseActivityTypeId.Should().BeNull();
        result.premiseActivityTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenLookupValueIsNull_ReturnsNullTuple()
    {
        // Arrange & Act
        var result = await _sut.FindAsync(null);

        // Assert
        result.premiseActivityTypeId.Should().BeNull();
        result.premiseActivityTypeName.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WhenCodeMatchesAndNameAlsoExists_PrioritizesCodeMatch()
    {
        // Arrange
        var premisesActivityTypes = new List<PremisesActivityTypeDocument>
        {
            new()
            {
                IdentifierId = "id1",
                Code = "CC",
                Name = "Collection Centre",
                IsActive = true,
                PriorityOrder = 30,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            },
            new()
            {
                IdentifierId = "id2",
                Code = "MARP",
                Name = "CC",
                IsActive = true,
                PriorityOrder = 10,
                EffectiveStartDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            }
        };

        var listDocument = new PremisesActivityTypeListDocument
        {
            Id = "all-premisesactivitytypes",
            PremisesActivityTypes = premisesActivityTypes
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.FindAsync("CC");

        // Assert
        result.premiseActivityTypeId.Should().Be("CC");
        result.premiseActivityTypeName.Should().Be("Collection Centre");
    }
}