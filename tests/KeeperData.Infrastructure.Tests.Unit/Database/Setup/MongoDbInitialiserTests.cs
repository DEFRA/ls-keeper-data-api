using KeeperData.Core.Attributes;
using KeeperData.Core.Repositories;
using KeeperData.Infrastructure.Services;
using KeeperData.Infrastructure.Tests.Unit.Database.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Setup;

public class MongoDbInitialiserTests
{
    private readonly MongoDbInitialiser _sut;
    private readonly Mock<IMongoIndexManager<BsonDocument>> _mockMongoIndexHandler;
    private readonly List<BsonDocument> _collectionIndexList = [];
    private readonly MockMongoDatabase _mockDb = new();
    private static readonly IEnumerable<CreateIndexModel<BsonDocument>> _typeIndexList =
    [
        new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("foo"),
            new CreateIndexOptions { Name = "still-declared" })
    ];

    public MongoDbInitialiserTests()
    {
        _mockMongoIndexHandler = new();
        _mockMongoIndexHandler
            .Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MockMongoDatabase.StubAsyncCursor<BsonDocument>(_collectionIndexList));

        _mockDb.SetupCollection<BsonDocument>(CollectionName);
        _mockDb.MockCollection<BsonDocument>().Setup(x => x.Indexes).Returns(_mockMongoIndexHandler.Object);

        _sut = new MongoDbInitialiser(_mockDb.Client, _mockDb.Config);
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
    public async Task WhenIInitialiseItShouldCreateIndexes()
    {
        _collectionIndexList.Add(BsonDocument.Parse("{\"name\":\"idx_thisisav1index\"}"));
        await _sut.Initialise(typeof(DocumentWithIndexes));
        _mockMongoIndexHandler.Verify(x => x.CreateManyAsync(_typeIndexList, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenIInitialise_ItShouldDropIndexesNoLongerDeclaredInCode()
    {
        _collectionIndexList.Add(BsonDocument.Parse("{\"name\":\"_id_\"}"));
        _collectionIndexList.Add(BsonDocument.Parse("{\"name\":\"still-declared\"}"));
        _collectionIndexList.Add(BsonDocument.Parse("{\"name\":\"idx_thisisav1index\"}"));

        await _sut.Initialise(typeof(DocumentWithIndexes));

        _mockMongoIndexHandler.Verify(x => x.DropOneAsync("idx_thisisav1index", It.IsAny<CancellationToken>()), Times.Once);
        _mockMongoIndexHandler.Verify(x => x.DropOneAsync("_id_", It.IsAny<CancellationToken>()), Times.Never);
        _mockMongoIndexHandler.Verify(x => x.DropOneAsync("still-declared", It.IsAny<CancellationToken>()), Times.Never);
    }
}