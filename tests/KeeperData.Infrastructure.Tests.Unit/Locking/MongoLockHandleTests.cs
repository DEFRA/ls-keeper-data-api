using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Locking;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Locking;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Locking;

public class MongoLockHandleTests
{
    private readonly Mock<IOptions<MongoConfig>> _mockMongoConfig;
    private readonly Mock<IMongoClient> _mockMongoClient;
    private readonly Mock<IMongoDatabase> _mockMongoDatabase;
    private readonly Mock<IMongoCollection<DistributedLock>> _mockCollection;
    private readonly Mock<IMongoIndexManager<DistributedLock>> _mockIndexManager;

    public MongoLockHandleTests()
    {
        _mockMongoConfig = new Mock<IOptions<MongoConfig>>();
        _mockMongoClient = new Mock<IMongoClient>();
        _mockMongoDatabase = new Mock<IMongoDatabase>();
        _mockCollection = new Mock<IMongoCollection<DistributedLock>>();
        _mockIndexManager = new Mock<IMongoIndexManager<DistributedLock>>();

        var mongoConfig = new MongoConfig { DatabaseName = "test-database" };
        _mockMongoConfig.Setup(x => x.Value).Returns(mongoConfig);
        _mockMongoClient.Setup(x => x.GetDatabase("test-database", null))
                       .Returns(_mockMongoDatabase.Object);
        _mockMongoDatabase.Setup(x => x.GetCollection<DistributedLock>("distributed_locks", null))
                         .Returns(_mockCollection.Object);
        _mockCollection.Setup(x => x.Indexes).Returns(_mockIndexManager.Object);
    }

    [Fact]
    public async Task LockHandle_TryRenewAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        using var distributedLock = new MongoDistributedLock(_mockMongoConfig.Object, _mockMongoClient.Object);
        var lockName = "test-lock";
        var duration = TimeSpan.FromMinutes(5);
        var extension = TimeSpan.FromMinutes(3);

        var expiredLock = new DistributedLock
        {
            Id = lockName,
            Owner = "previous-owner",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(x => x.ModifiedCount).Returns(1);

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredLock);

        _mockCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<UpdateDefinition<DistributedLock>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult.Object);

        // Act
        var handle = await distributedLock.TryAcquireAsync(lockName, duration);
        var renewResult = await handle!.TryRenewAsync(extension);

        // Assert
        renewResult.Should().BeTrue();
        _mockCollection.Verify(x => x.UpdateOneAsync(
            It.IsAny<FilterDefinition<DistributedLock>>(),
            It.IsAny<UpdateDefinition<DistributedLock>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LockHandle_TryRenewAsync_WhenLockNotFound_ReturnsFalse()
    {
        // Arrange
        using var distributedLock = new MongoDistributedLock(_mockMongoConfig.Object, _mockMongoClient.Object);
        var lockName = "test-lock";
        var duration = TimeSpan.FromMinutes(5);
        var extension = TimeSpan.FromMinutes(3);

        var expiredLock = new DistributedLock
        {
            Id = lockName,
            Owner = "previous-owner",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var updateResult = new Mock<UpdateResult>();
        updateResult.Setup(x => x.ModifiedCount).Returns(0);

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredLock);

        _mockCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<UpdateDefinition<DistributedLock>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult.Object);

        // Act
        var handle = await distributedLock.TryAcquireAsync(lockName, duration);
        var renewResult = await handle!.TryRenewAsync(extension);

        // Assert
        renewResult.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    public async Task LockHandle_TryRenewAsync_WhenExtensionIsInvalid_ThrowsArgumentOutOfRangeException(int extensionSeconds)
    {
        // Arrange
        using var distributedLock = new MongoDistributedLock(_mockMongoConfig.Object, _mockMongoClient.Object);
        var lockName = "test-lock";
        var duration = TimeSpan.FromMinutes(5);
        var extension = TimeSpan.FromSeconds(extensionSeconds);

        var expiredLock = new DistributedLock
        {
            Id = lockName,
            Owner = "previous-owner",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredLock);

        // Act
        var handle = await distributedLock.TryAcquireAsync(lockName, duration);

        // Assert
        await handle!.Invoking(h => h.TryRenewAsync(extension))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task LockHandle_TryRenewAsync_WhenMongoExceptionThrown_ReturnsFalse()
    {
        // Arrange
        using var distributedLock = new MongoDistributedLock(_mockMongoConfig.Object, _mockMongoClient.Object);
        var lockName = "test-lock";
        var duration = TimeSpan.FromMinutes(5);
        var extension = TimeSpan.FromMinutes(3);

        var expiredLock = new DistributedLock
        {
            Id = lockName,
            Owner = "previous-owner",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredLock);

        _mockCollection.Setup(x => x.UpdateOneAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<UpdateDefinition<DistributedLock>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Update failed"));

        // Act
        var handle = await distributedLock.TryAcquireAsync(lockName, duration);
        var renewResult = await handle!.TryRenewAsync(extension);

        // Assert
        renewResult.Should().BeFalse();
    }

    [Fact]
    public async Task LockHandle_DisposeAsync_DeletesLockDocument()
    {
        // Arrange
        using var distributedLock = new MongoDistributedLock(_mockMongoConfig.Object, _mockMongoClient.Object);
        var lockName = "test-lock";
        var duration = TimeSpan.FromMinutes(5);

        var expiredLock = new DistributedLock
        {
            Id = lockName,
            Owner = "previous-owner",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var deleteResult = new Mock<DeleteResult>();
        deleteResult.Setup(x => x.DeletedCount).Returns(1);

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredLock);

        _mockCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteResult.Object);

        // Act
        var handle = await distributedLock.TryAcquireAsync(lockName, duration);
        await handle!.DisposeAsync();

        // Assert
        _mockCollection.Verify(x => x.DeleteOneAsync(
            It.IsAny<FilterDefinition<DistributedLock>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LockHandle_DisposeAsync_WhenMongoExceptionThrown_IgnoresException()
    {
        // Arrange
        using var distributedLock = new MongoDistributedLock(_mockMongoConfig.Object, _mockMongoClient.Object);
        var lockName = "test-lock";
        var duration = TimeSpan.FromMinutes(5);

        var expiredLock = new DistributedLock
        {
            Id = lockName,
            Owner = "previous-owner",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredLock);

        _mockCollection.Setup(x => x.DeleteOneAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MongoException("Delete failed"));

        // Act & Assert - Should not throw
        var handle = await distributedLock.TryAcquireAsync(lockName, duration);
        Func<Task> act = async () => await handle!.DisposeAsync();
        await act.Should().NotThrowAsync();
    }
}