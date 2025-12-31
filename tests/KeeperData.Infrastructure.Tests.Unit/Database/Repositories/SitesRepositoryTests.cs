using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class SitesRepositoryTests
{
    private readonly Mock<IOptions<MongoConfig>> _mongoConfigMock = new();
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IMongoCollection<SiteDocument>> _mongoCollectionMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IAsyncCursor<SiteDocument>> _asyncCursorMock = new();

    private readonly MongoConfig _mongoConfig = new() { DatabaseName = "TestDatabase" };

    private SitesRepository CreateSut()
    {
        _mongoConfigMock.Setup(x => x.Value).Returns(_mongoConfig);
        _mongoClientMock.Setup(x => x.GetDatabase(_mongoConfig.DatabaseName, null))
            .Returns(_mongoDatabaseMock.Object);
        _mongoDatabaseMock.Setup(x => x.GetCollection<SiteDocument>("SiteDocument", null))
            .Returns(_mongoCollectionMock.Object);

        return new SitesRepository(
            _mongoConfigMock.Object,
            _mongoClientMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnDocumentCount_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<SiteDocument>.Filter.Empty;
        var expectedCount = 5L;

        _mongoCollectionMock
            .Setup(x => x.CountDocumentsAsync(filter, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await sut.CountAsync(filter);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task CountAsync_ShouldUseCancellationToken_WhenProvided()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<SiteDocument>.Filter.Empty;
        var cancellationToken = new CancellationToken();
        var expectedCount = 3L;

        _mongoCollectionMock
            .Setup(x => x.CountDocumentsAsync(filter, null, cancellationToken))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await sut.CountAsync(filter, cancellationToken);

        // Assert
        result.Should().Be(3);
        _mongoCollectionMock.Verify(x => x.CountDocumentsAsync(filter, null, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ShouldReturnFilteredAndSortedDocuments_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<SiteDocument>.Filter.Empty;
        var sort = Builders<SiteDocument>.Sort.Ascending(x => x.Id);
        var skip = 10;
        var take = 20;

        var expectedDocuments = new List<SiteDocument>
        {
            new() { Id = "site1" },
            new() { Id = "site2" }
        };

        var mockFindFluent = new Mock<IFindFluent<SiteDocument, SiteDocument>>();
        mockFindFluent.Setup(x => x.Sort(sort)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.Skip(skip)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.Limit(take)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(expectedDocuments));

        _mongoCollectionMock.Setup(x => x.Find(filter, null))
            .Returns(mockFindFluent.Object);

        // Act
        var result = await sut.FindAsync(filter, sort, skip, take);

        // Assert
        result.Should().BeEquivalentTo(expectedDocuments);
        mockFindFluent.Verify(x => x.Sort(sort), Times.Once);
        mockFindFluent.Verify(x => x.Skip(skip), Times.Once);
        mockFindFluent.Verify(x => x.Limit(take), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ShouldUseCancellationToken_WhenProvided()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<SiteDocument>.Filter.Empty;
        var sort = Builders<SiteDocument>.Sort.Ascending(x => x.Id);
        var skip = 0;
        var take = 10;
        var cancellationToken = new CancellationToken();

        var expectedDocuments = new List<SiteDocument>
        {
            new() { Id = "site1" }
        };

        var mockFindFluent = new Mock<IFindFluent<SiteDocument, SiteDocument>>();
        mockFindFluent.Setup(x => x.Sort(sort)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.Skip(skip)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.Limit(take)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.ToListAsync(cancellationToken))
            .Returns(Task.FromResult(expectedDocuments));

        _mongoCollectionMock.Setup(x => x.Find(filter, null))
            .Returns(mockFindFluent.Object);

        // Act
        var result = await sut.FindAsync(filter, sort, skip, take, cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expectedDocuments);
        mockFindFluent.Verify(x => x.ToListAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ShouldReturnEmptyList_WhenNoDocumentsFound()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<SiteDocument>.Filter.Empty;
        var sort = Builders<SiteDocument>.Sort.Ascending(x => x.Id);
        var skip = 0;
        var take = 10;

        var expectedDocuments = new List<SiteDocument>();

        var mockFindFluent = new Mock<IFindFluent<SiteDocument, SiteDocument>>();
        mockFindFluent.Setup(x => x.Sort(sort)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.Skip(skip)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.Limit(take)).Returns(mockFindFluent.Object);
        mockFindFluent.Setup(x => x.ToListAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(expectedDocuments));

        _mongoCollectionMock.Setup(x => x.Find(filter, null))
            .Returns(mockFindFluent.Object);

        // Act
        var result = await sut.FindAsync(filter, sort, skip, take);

        // Assert
        result.Should().BeEmpty();
    }
}