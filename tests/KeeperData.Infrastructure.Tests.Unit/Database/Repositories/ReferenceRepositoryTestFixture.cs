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

public class ReferenceRepositoryTestFixture<TSut, TListDocument, TItem> where TListDocument : class, IReferenceListDocument<TItem> where TSut : IReferenceDataRepository<TListDocument, TItem>
    {
        private readonly Mock<IOptions<MongoConfig>> _configMock;
        private readonly Mock<IMongoClient> _mongoClientMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IClientSessionHandle> _clientSessionHandleMock;
        private readonly Mock<IMongoCollection<TListDocument>> _collectionMock;
        private readonly Mock<IAsyncCursor<TListDocument>> _asyncCursorMock;

        public ReferenceRepositoryTestFixture()
        {
            _configMock = new Mock<IOptions<MongoConfig>>();
            _mongoClientMock = new Mock<IMongoClient>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _clientSessionHandleMock = new Mock<IClientSessionHandle>();
            _collectionMock = new Mock<IMongoCollection<TListDocument>>();
            _asyncCursorMock = new Mock<IAsyncCursor<TListDocument>>();

            _configMock.Setup(c => c.Value).Returns(new MongoConfig { DatabaseName = "test" });

            var mockDatabase = new Mock<IMongoDatabase>();
            _mongoClientMock.Setup(c => c.GetDatabase(It.IsAny<string>(), null)).Returns(mockDatabase.Object);
            mockDatabase.Setup(d => d.GetCollection<TListDocument>(It.IsAny<string>(), null))
                .Returns(_collectionMock.Object);

            _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .Callback(() =>
                {
                    _asyncCursorMock.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
                });

            _collectionMock.Setup(c => c.FindAsync(
                    It.IsAny<IClientSessionHandle?>(),
                    It.IsAny<FilterDefinition<TListDocument>>(),
                    It.IsAny<FindOptions<TListDocument, TListDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_asyncCursorMock.Object);

            _unitOfWorkMock.Setup(u => u.Session).Returns(_clientSessionHandleMock.Object);
        }

        public TSut CreateSut(Func<IOptions<MongoConfig>, IMongoClient, IUnitOfWork, TSut> sutConstructor)
        {
            var sut = sutConstructor(_configMock.Object, _mongoClientMock.Object, _unitOfWorkMock.Object);

            var collectionField = typeof(FacilityBusinessActivityMapRepository).BaseType!.BaseType!
                .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
            collectionField!.SetValue(sut, _collectionMock.Object);
            return sut;
        }

        public void SetUpDocuments(TListDocument documentList)
        {
            _asyncCursorMock.SetupGet(c => c.Current).Returns([documentList]);
        }
    }
