using FluentAssertions;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Moq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class GenericRepositoryTests
{
    private readonly IOptions<MongoConfig> _mongoConfig;
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IAsyncCursor<TestEntity>> _asyncCursorMock = new();
    private readonly Mock<IMongoCollection<TestEntity>> _mongoCollectionMock = new();

    private readonly GenericRepository<TestEntity> _sut;

    public GenericRepositoryTests()
    {
        _mongoConfig = Options.Create(new MongoConfig { DatabaseName = "DatabaseName" });

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<TestEntity>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
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
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<FindOptions<TestEntity, TestEntity>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _unitOfWorkMock.Setup(u => u.Session)
            .Returns(_clientSessionHandleMock.Object);

        _sut = new GenericRepository<TestEntity>(_mongoConfig, _mongoClientMock.Object, _unitOfWorkMock.Object);

        typeof(GenericRepository<TestEntity>)
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_sut, _mongoCollectionMock.Object);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenValidId_WhenCallingGetByIdAsync_ThenReturnsExpectedEntity(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var expected = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "Test Entity" };

        _asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns([expected]);

        var result = await _sut.GetByIdAsync(expected.Id, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenValidId_WhenCallingFindOneAsync_ThenReturnsExpectedEntity(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var expected = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "Test Entity" };

        _asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns([expected]);

        var result = await _sut.FindOneAsync(x => x.Id == expected.Id, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenEntity_WhenCallingAddAsync_ThenInsertOneIsCalled(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var entity = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "New Entity" };

        _mongoCollectionMock
            .Setup(c => c.InsertOneAsync(
                It.IsAny<IClientSessionHandle?>(),
                entity,
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _sut.AddAsync(entity, CancellationToken.None);

        _mongoCollectionMock.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenEntity_WhenCallingAddManyAsync_ThenInsertManyAsyncCalled(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var entity = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "New Entity" };
        IEnumerable<TestEntity> entities = [entity];

        _mongoCollectionMock
            .Setup(c => c.InsertManyAsync(
                It.IsAny<IClientSessionHandle?>(),
                entities,
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _sut.AddManyAsync([entity], CancellationToken.None);

        _mongoCollectionMock.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenEntity_WhenCallingUpdateAsync_ThenReplaceOneIsCalled(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var entity = new TestEntity { Id = Guid.NewGuid().ToString(), Name = "Updated Entity" };

        var replaceResultMock = new Mock<ReplaceOneResult>();
        replaceResultMock.SetupAllProperties();

        _mongoCollectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<IClientSessionHandle?>(),
                It.IsAny<FilterDefinition<TestEntity>>(),
                entity,
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaceResultMock.Object)
            .Verifiable();

        await _sut.UpdateAsync(entity, CancellationToken.None);

        _mongoCollectionMock.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenEntities_WhenCallingBulkUpsertAsync_ThenBulkWriteIsCalledWithUpsert(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid().ToString(), Name = "Test Entity 1" },
            new TestEntity { Id = Guid.NewGuid().ToString(), Name = "Test Entity 2" }
        };

        IEnumerable<WriteModel<TestEntity>>? capturedModels = null;
        _mongoCollectionMock.Setup(c => c.BulkWriteAsync(
            It.IsAny<IClientSessionHandle?>(),
            It.IsAny<IEnumerable<WriteModel<TestEntity>>>(),
            null,
            It.IsAny<CancellationToken>()))
            .Callback<IClientSessionHandle?, IEnumerable<WriteModel<TestEntity>>, BulkWriteOptions?, CancellationToken>((_, models, _, _) =>
            {
                capturedModels = models;
            })
            .ReturnsAsync((BulkWriteResult<TestEntity>?)null);

        await _sut.BulkUpsertAsync(entities, CancellationToken.None);

        _mongoCollectionMock.Verify(c => c.BulkWriteAsync(
            It.IsAny<IClientSessionHandle?>(),
            It.IsAny<IEnumerable<WriteModel<TestEntity>>>(),
            null,
            It.IsAny<CancellationToken>()),
            Times.Once);

        capturedModels.Should().NotBeNull().And.HaveCount(2);
        capturedModels!.All(m => m is ReplaceOneModel<TestEntity> model
            && model.IsUpsert
            && entities.Any(e => e.Id == model.Replacement.Id)).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenFilteredEntities_WhenCallingBulkUpsertWithCustomFilterAsync_ThenBulkWriteIsCalledWithUpsert(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var items = new (FilterDefinition<TestEntity> Filter, TestEntity Entity)[]
        {
            (Builders<TestEntity>.Filter.Eq(x => x.Name, "One"), new TestEntity { Id = "1", Name = "One" }),
            (Builders<TestEntity>.Filter.Eq(x => x.Name, "Two"), new TestEntity { Id = "2", Name = "Two" })
        };

        IEnumerable<WriteModel<TestEntity>>? capturedModels = null;
        _mongoCollectionMock.Setup(c => c.BulkWriteAsync(
            It.IsAny<IClientSessionHandle?>(),
            It.IsAny<IEnumerable<WriteModel<TestEntity>>>(),
            null,
            It.IsAny<CancellationToken>()))
            .Callback<IClientSessionHandle?, IEnumerable<WriteModel<TestEntity>>, BulkWriteOptions?, CancellationToken>((_, models, _, _) =>
            {
                capturedModels = models;
            })
            .ReturnsAsync((BulkWriteResult<TestEntity>?)null);

        await _sut.BulkUpsertWithCustomFilterAsync(items, CancellationToken.None);

        _mongoCollectionMock.Verify(c => c.BulkWriteAsync(
            It.IsAny<IClientSessionHandle?>(),
            It.IsAny<IEnumerable<WriteModel<TestEntity>>>(),
            null,
            It.IsAny<CancellationToken>()),
            Times.Once);

        capturedModels.Should().NotBeNull().And.HaveCount(2);
        capturedModels!.All(m =>
        {
            return m is ReplaceOneModel<TestEntity> r
                && r.IsUpsert
                && items.Any(i => i.Entity.Id == r.Replacement.Id
                    && i.Entity.Name == r.Replacement.Name);
        }).Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenValidId_WhenCallingDeleteAsync_ThenDeleteOneIsCalled(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var id = Guid.NewGuid().ToString();

        _mongoCollectionMock
            .Setup(c => c.DeleteOneAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<DeleteResult>())
            .Verifiable();

        await _sut.DeleteAsync(id, CancellationToken.None);

        _mongoCollectionMock.Verify();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenValidId_WhenCallingDeleteManyAsync_ThenDeleteManyIsCalled(bool useTransaction)
    {
        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(useTransaction);

        var id = Guid.NewGuid().ToString();

        _mongoCollectionMock
            .Setup(c => c.DeleteManyAsync(
                It.IsAny<IClientSessionHandle>(),
                It.IsAny<FilterDefinition<TestEntity>>(),
                It.IsAny<DeleteOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<DeleteResult>())
            .Verifiable();

        var deleteFilter = Builders<TestEntity>.Filter.In(x => x.Id, [id]);

        await _sut.DeleteManyAsync(id, CancellationToken.None);

        _mongoCollectionMock.Verify();
    }

    [Fact]
    public void AllDocumentsShouldHaveMatchingJsonAndBsonAttributes()
    {
        var assembly = Assembly.GetAssembly(typeof(PartyDocument));
        var documentTypes = assembly?.GetTypes().Where(t => t.IsAssignableTo(typeof(IEntity)) || t.IsAssignableTo(typeof(INestedEntity)));
        documentTypes.Should().NotBeNull();
        foreach (var type in documentTypes)
        {
            var properties = type.GetMembers()
            .Where(p => p.IsDefined(typeof(BsonElementAttribute)))
            .Select(member =>
            new
            {
                member,
                bsonAttribute = member.GetCustomAttribute<BsonElementAttribute>(),
                jsonAttribute = member.GetCustomAttribute<JsonPropertyNameAttribute>()
            })
            .ToList();
            foreach (var property in properties)
            {
                property.jsonAttribute.Should().NotBeNull($"Class {type.Name} member {property.member.Name} must have a JsonPropertyNameAttribute if it has a BsonElementAttribute");
                property.jsonAttribute.Name.Should().Be(property.bsonAttribute?.ElementName, $"Class {type.Name} member {property.member.Name} should have matching JsonPropertyNameAttribute '{property.jsonAttribute.Name}' and BsonElementAttribute '{property.bsonAttribute?.ElementName}'");
            }
        }
    }

    [Fact]
    public void AllAutoIndexedDocumentsShouldHaveBsonAttribute()
    {
        var assembly = Assembly.GetAssembly(typeof(PartyDocument));
        var documentTypes = assembly?.GetTypes().Where(t => t.IsAssignableTo(typeof(IEntity)) || t.IsAssignableTo(typeof(INestedEntity)));
        documentTypes.Should().NotBeNull();
        foreach (var type in documentTypes)
        {
            var properties = type.GetMembers()
            .Where(p => p.IsDefined(typeof(AutoIndexedAttribute)))
            .Select(member =>
            new
            {
                member,
                indexAttribute = member.GetCustomAttribute<AutoIndexedAttribute>(),
                bsonAttribute = member.GetCustomAttribute<BsonElementAttribute>()
            })
            .ToList();
            foreach (var property in properties)
            {
                property.bsonAttribute.Should().NotBeNull($"Class {type.Name} member {property.member.Name} must have a BsonPropertyNameAttribute if it has a AutoIndexedAttribute");
            }
        }
    }
}

[CollectionName("TestEntities")]
public class TestEntity : IEntity
{
    public string Id { get; set; } = default!;
    public int? LastUpdatedBatchId { get; set; }
    public string Name { get; set; } = default!;
}