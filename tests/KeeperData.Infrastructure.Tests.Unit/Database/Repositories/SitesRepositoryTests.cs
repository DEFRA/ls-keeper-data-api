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
        _mongoDatabaseMock.Setup(x => x.GetCollection<SiteDocument>("sites", null))
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
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<SiteDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
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
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<SiteDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await sut.CountAsync(filter, cancellationToken);

        // Assert
        result.Should().Be(3);
        _mongoCollectionMock.Verify(x => x.CountDocumentsAsync(
            It.IsAny<FilterDefinition<SiteDocument>>(),
            It.IsAny<CountOptions>(),
            cancellationToken), Times.Once);
    }
}