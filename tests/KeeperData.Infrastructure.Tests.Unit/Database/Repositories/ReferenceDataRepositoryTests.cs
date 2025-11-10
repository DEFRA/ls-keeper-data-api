using FluentAssertions;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class ReferenceDataRepositoryTests
{
    private readonly IOptions<MongoConfig> _mongoConfig;
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IAsyncCursor<TestReferenceListDocument>> _asyncCursorMock = new();
    private readonly Mock<IMongoCollection<TestReferenceListDocument>> _mongoCollectionMock = new();

    private readonly TestReferenceRepository _sut;

    public ReferenceDataRepositoryTests()
    {
        _mongoConfig = Options.Create(new MongoConfig { DatabaseName = "TestDatabase" });

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<TestReferenceListDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
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
                It.IsAny<FilterDefinition<TestReferenceListDocument>>(),
                It.IsAny<FindOptions<TestReferenceListDocument, TestReferenceListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session)
            .Returns(_clientSessionHandleMock.Object);

        _sut = new TestReferenceRepository(_mongoConfig, _mongoClientMock.Object, _unitOfWorkMock.Object);

        typeof(GenericRepository<TestReferenceListDocument>)
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_sut, _mongoCollectionMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_LoadsItemsOnFirstCall()
    {
        // Arrange
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(false);

        var items = new List<TestReferenceDocument>
        {
            new() { IdentifierId = "1", Name = "Item 1" },
            new() { IdentifierId = "2", Name = "Item 2" }
        };

        var listDocument = new TestReferenceListDocument
        {
            Id = "test-list",
            TestItems = items
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCachedItemsOnSubsequentCalls()
    {
        // Arrange
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(false);

        var items = new List<TestReferenceDocument>
        {
            new() { IdentifierId = "1", Name = "Item 1" }
        };

        var listDocument = new TestReferenceListDocument
        {
            Id = "test-list",
            TestItems = items
        };

        _asyncCursorMock.SetupGet(c => c.Current).Returns([listDocument]);

        var findCallCount = 0;
        _mongoCollectionMock
            .Setup(c => c.FindAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<TestReferenceListDocument>>(),
                It.IsAny<FindOptions<TestReferenceListDocument, TestReferenceListDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => findCallCount++)
            .ReturnsAsync(_asyncCursorMock.Object);

        // Act
        await _sut.GetAllAsync(CancellationToken.None);
        await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        findCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyCollection_WhenNoDocumentFound()
    {
        // Arrange
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(false);

        _asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}

// Test implementation classes
public class TestReferenceDocument : INestedEntity
{
    public string IdentifierId { get; set; } = default!;
    public string Name { get; set; } = default!;
}

[CollectionName("testReferenceData")]
public class TestReferenceListDocument : IReferenceListDocument<TestReferenceDocument>, IListDocument
{
    public static string DocumentId => "test-list";

    public string Id { get; set; } = DocumentId;
    public int LastUpdatedBatchId { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public List<TestReferenceDocument> TestItems { get; set; } = [];
    public IReadOnlyCollection<TestReferenceDocument> Items => TestItems.AsReadOnly();
}

public class TestReferenceRepository : ReferenceDataRepository<TestReferenceListDocument, TestReferenceDocument>
{
    public TestReferenceRepository(
        IOptions<MongoConfig> mongoConfig,
        IMongoClient client,
        IUnitOfWork unitOfWork)
        : base(mongoConfig, client, unitOfWork)
    {
    }
}