using KeeperData.Core.Attributes;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class MockMongoDatabase
{
    private readonly MongoConfig _mongoConfig = new() { DatabaseName = "TestDatabase" };
    private readonly Mock<IOptions<MongoConfig>> _mongoConfigMock = new();
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Dictionary<Type, object> _mongoCollectionMocks = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IClientSessionHandle> _clientSessionHandleMock = new();

    public MockMongoDatabase()
    {
        _mongoConfigMock.Setup(x => x.Value).Returns(_mongoConfig);
        _mongoClientMock.Setup(x => x.GetDatabase(_mongoConfig.DatabaseName, null))
            .Returns(_mongoDatabaseMock.Object);

        _clientSessionHandleMock.Setup(s => s.IsInTransaction).Returns(false);
        _unitOfWorkMock.Setup(u => u.Session).Returns(_clientSessionHandleMock.Object);
    }

    public IOptions<MongoConfig> Config => _mongoConfigMock.Object;
    public IMongoClient Client => _mongoClientMock.Object;
    public IUnitOfWork UnitOfWork => _unitOfWorkMock.Object;

    public void SetupCollection<T>(string? collectionName = null)
    {
        var collectionMock = new Mock<IMongoCollection<T>>();
        collectionName ??= GetCollectionName<T>();
        _mongoDatabaseMock
            .Setup(x => x.GetCollection<T>(collectionName, null))
            .Returns(collectionMock.Object);

        _mongoCollectionMocks.Add(typeof(T), collectionMock);
    }

    private string GetCollectionName<T>()
    {
        var type = typeof(T);
        return type.GetCustomAttribute<CollectionNameAttribute>()?.Name ?? type.Name;
    }

    public Mock<IMongoCollection<T>> MockCollection<T>()
    {
        return (Mock<IMongoCollection<T>>)_mongoCollectionMocks[typeof(T)];
    }

    public class StubAsyncCursor<T> : IAsyncCursor<T>
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
}