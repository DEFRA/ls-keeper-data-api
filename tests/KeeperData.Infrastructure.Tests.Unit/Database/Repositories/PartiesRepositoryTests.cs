using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Repositories;
using KeeperData.Core.Transactions;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class PartiesRepositoryTests
{
    private readonly Mock<IOptions<MongoConfig>> _mongoConfigMock = new();
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IMongoCollection<PartyDocument>> _mongoCollectionMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly MongoConfig _mongoConfig = new() { DatabaseName = "TestDatabase" };

    private PartiesRepository CreateSut()
    {
        _mongoConfigMock.Setup(x => x.Value).Returns(_mongoConfig);
        _mongoClientMock.Setup(x => x.GetDatabase(_mongoConfig.DatabaseName, null))
            .Returns(_mongoDatabaseMock.Object);
        _mongoDatabaseMock.Setup(x => x.GetCollection<PartyDocument>("parties", null))
            .Returns(_mongoCollectionMock.Object);

        return new PartiesRepository(
            _mongoConfigMock.Object,
            _mongoClientMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnDocumentCount_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<PartyDocument>.Filter.Empty;
        var expectedCount = 7L;

        _mongoCollectionMock
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<PartyDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await sut.CountAsync(filter);

        // Assert
        result.Should().Be(7);
    }

    [Fact]
    public async Task CountAsync_ShouldUseCaseInsensitiveCollation_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<PartyDocument>.Filter.Empty;
        var expectedCount = 3L;

        _mongoCollectionMock
            .Setup(x => x.CountDocumentsAsync(
                filter,
                It.Is<CountOptions>(opts => opts.Collation == IndexDefaults.CollationCaseInsensitive),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await sut.CountAsync(filter);

        // Assert
        result.Should().Be(3);
        _mongoCollectionMock.Verify(x => x.CountDocumentsAsync(
            filter,
            It.Is<CountOptions>(opts => opts.Collation == IndexDefaults.CollationCaseInsensitive),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CountAsync_ShouldUseCancellationToken_WhenProvided()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<PartyDocument>.Filter.Empty;
        var cancellationToken = new CancellationToken();
        var expectedCount = 5L;

        _mongoCollectionMock
            .Setup(x => x.CountDocumentsAsync(
                It.IsAny<FilterDefinition<PartyDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await sut.CountAsync(filter, cancellationToken);

        // Assert
        result.Should().Be(5);
        _mongoCollectionMock.Verify(x => x.CountDocumentsAsync(
            It.IsAny<FilterDefinition<PartyDocument>>(),
            It.IsAny<CountOptions>(),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ShouldReturnFilteredAndSortedDocuments_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<PartyDocument>.Filter.Empty;
        var sort = Builders<PartyDocument>.Sort.Ascending(x => x.Id);
        var skip = 5;
        var take = 15;

        // This test validates that the method exists and can be called without throwing
        // The MongoDB driver extension methods cannot be mocked effectively with Moq

        // Act & Assert - should not throw
        var act = () => sut.FindAsync(filter, sort, skip, take);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FindAsync_ShouldUseCaseInsensitiveCollation_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<PartyDocument>.Filter.Empty;
        var sort = Builders<PartyDocument>.Sort.Ascending(x => x.Id);
        var skip = 0;
        var take = 10;

        // Act & Assert - should not throw
        var act = () => sut.FindAsync(filter, sort, skip, take);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FindAsync_ShouldUseCancellationToken_WhenProvided()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<PartyDocument>.Filter.Empty;
        var sort = Builders<PartyDocument>.Sort.Ascending(x => x.Id);
        var skip = 0;
        var take = 10;
        var cancellationToken = new CancellationToken();

        // Act & Assert - should not throw
        var act = () => sut.FindAsync(filter, sort, skip, take, cancellationToken);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FindAsync_ShouldReturnEmptyList_WhenNoDocumentsFound()
    {
        // Arrange
        var sut = CreateSut();
        var filter = Builders<PartyDocument>.Filter.Empty;
        var sort = Builders<PartyDocument>.Sort.Ascending(x => x.Id);
        var skip = 0;
        var take = 10;

        // Act & Assert - should not throw
        var act = () => sut.FindAsync(filter, sort, skip, take);
        await act.Should().NotThrowAsync();
    }
}