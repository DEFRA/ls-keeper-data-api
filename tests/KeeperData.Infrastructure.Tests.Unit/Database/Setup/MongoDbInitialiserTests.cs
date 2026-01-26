using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Setup;

public class MongoDbInitialiserTests
{
    private readonly MongoDbInitialiser _sut;
    private readonly Mock<IMongoIndexManager<BsonDocument>> _mockMongoIndexHandler;
    private readonly List<BsonDocument> _collectionIndexList = [];
    private static readonly IEnumerable<CreateIndexModel<BsonDocument>> _typeIndexList = [];

    private class StubAsyncCursor<T> : IAsyncCursor<T>
    {
        private List<T>.Enumerator _enumerator;

        public StubAsyncCursor(List<T> inner)
        {
            _enumerator = inner.GetEnumerator();
        }

        public void Dispose()
        {
        }

        public bool MoveNext(CancellationToken cancellationToken = new CancellationToken())
        {
            return _enumerator.MoveNext();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(_enumerator.MoveNext());
        }

        public IEnumerable<T> Current => [_enumerator.Current];
    }

    public MongoDbInitialiserTests()
    {
        var mongoClientMock = new Mock<IMongoClient>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var clientSessionHandleMock = new Mock<IClientSessionHandle>();
        var collectionMock = new Mock<IMongoCollection<BsonDocument>>();
        Mock<IOptions<MongoConfig>> configMock = new();
        _mockMongoIndexHandler = new();

        configMock.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "test" });
        clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(false);

        var mockDatabase = new Mock<IMongoDatabase>();
        mongoClientMock.Setup(c => c.GetDatabase(It.IsAny<string>(), null)).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.GetCollection<BsonDocument>(CollectionName, null))
            .Returns(collectionMock.Object);

        unitOfWorkMock.Setup(u => u.Session).Returns(clientSessionHandleMock.Object);
        collectionMock.Setup(x => x.Indexes).Returns(_mockMongoIndexHandler.Object);

        _mockMongoIndexHandler
            .Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new StubAsyncCursor<BsonDocument>(_collectionIndexList));

        _sut = new MongoDbInitialiser(mongoClientMock.Object, configMock.Object);
    }

    private class DocumentWithoutIndexes
    {
    }

    private const string CollectionName = "collection-name";
    [CollectionName(CollectionName)]
    private class DocumentWithIndexes : IContainsIndexes
    {
        public static IEnumerable<CreateIndexModel<BsonDocument>> GetIndexModels()
        {
            return _typeIndexList;
        }
    }

    [Fact]
    public async Task WhenIInitialiseAnObjectWithoutIndexes_ItShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _sut.Initialise(typeof(DocumentWithoutIndexes));
        });
    }

    [Fact]
    public async Task WhenIInitialiseItShouldDropV1Index()
    {
        _collectionIndexList.Add(BsonDocument.Parse("{\"name\":\"idx_thisisav1index\"}"));
        await _sut.Initialise(typeof(DocumentWithIndexes));
        _mockMongoIndexHandler.Verify(x => x.DropOneAsync("idx_thisisav1index", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenIInitialiseItShouldCreateIndexes()
    {
        _collectionIndexList.Add(BsonDocument.Parse("{\"name\":\"idx_thisisav1index\"}"));
        await _sut.Initialise(typeof(DocumentWithIndexes));
        _mockMongoIndexHandler.Verify(x => x.CreateManyAsync(_typeIndexList, It.IsAny<CancellationToken>()), Times.Once);
    }
}