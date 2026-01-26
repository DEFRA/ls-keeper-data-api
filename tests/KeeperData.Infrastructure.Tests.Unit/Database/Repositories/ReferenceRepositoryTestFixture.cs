using KeeperData.Core.Documents.Reference;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class ReferenceRepositoryTestFixture<TSut, TListDocument, TItem> where TListDocument : class, IReferenceListDocument<TItem> where TSut : IReferenceDataRepository<TListDocument, TItem>
{
    private readonly MockMongoDatabase _mockDb;
    public Mock<IMongoCollection<TListDocument>> CollectionMock => _mockDb.MockCollection<TListDocument>();
    
    public ReferenceRepositoryTestFixture()
    {
        _mockDb = new MockMongoDatabase();
        _mockDb.SetupCollection<TListDocument>();
    }

    public TSut CreateSut(Func<IOptions<MongoConfig>, IMongoClient, IUnitOfWork, TSut> sutConstructor)
    {
        var sut = sutConstructor(_mockDb.Config, _mockDb.Client, _mockDb.UnitOfWork);

        var collectionField = typeof(TSut).BaseType!.BaseType!
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
        collectionField!.SetValue(sut, _mockDb.MockCollection<TListDocument>().Object);
        return sut;
    }

    public void SetUpNoDocuments()
    {
        SetUpDocuments([]);
    }
    
    public void SetUpDocuments(TListDocument documentList)
    {
        SetUpDocuments([documentList]);
    }

    private void SetUpDocuments(List<TListDocument> documentList)
    {
        _mockDb.MockCollection<TListDocument>()
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<TListDocument>>(),
                It.IsAny<FindOptions<TListDocument, TListDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MockMongoDatabase.StubAsyncCursor<TListDocument>(documentList));
    }
}