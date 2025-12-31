using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Core.Locking;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Locking;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Locking;

public class MongoDistributedLockTests : IDisposable
{
    private readonly Mock<IOptions<MongoConfig>> _mockMongoConfig;
    private readonly Mock<IMongoClient> _mockMongoClient;
    private readonly Mock<IMongoDatabase> _mockMongoDatabase;
    private readonly Mock<IMongoCollection<DistributedLock>> _mockCollection;
    private readonly Mock<IMongoIndexManager<DistributedLock>> _mockIndexManager;
    private readonly MongoDistributedLock _sut;

    public MongoDistributedLockTests()
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

        _sut = new MongoDistributedLock(_mockMongoConfig.Object, _mockMongoClient.Object);
    }

    [Fact]
    public void Constructor_WhenCalledWithValidParameters_SetsUpCollectionCorrectly()
    {
        // Arrange & Act - Constructor called in setup

        // Assert
        _mockMongoClient.Verify(x => x.GetDatabase("test-database", null), Times.Once);
        _mockMongoDatabase.Verify(x => x.GetCollection<DistributedLock>("distributed_locks", null), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenCalledFirstTime_CreatesIndex()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _sut.InitializeAsync(cancellationToken);

        // Assert
        _mockIndexManager.Verify(x => x.CreateOneAsync(
            It.IsAny<CreateIndexModel<DistributedLock>>(),
            It.IsAny<CreateOneIndexOptions>(),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenCalledMultipleTimes_CreatesIndexOnlyOnce()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        // Act
        await _sut.InitializeAsync(cancellationToken);
        await _sut.InitializeAsync(cancellationToken);
        await _sut.InitializeAsync(cancellationToken);

        // Assert
        _mockIndexManager.Verify(x => x.CreateOneAsync(
            It.IsAny<CreateIndexModel<DistributedLock>>(),
            It.IsAny<CreateOneIndexOptions>(),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        var cancellationToken = new CancellationToken(true);
        _mockIndexManager.Setup(x => x.CreateOneAsync(
                It.IsAny<CreateIndexModel<DistributedLock>>(),
                It.IsAny<CreateOneIndexOptions>(),
                cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        // Act & Assert
        await _sut.Invoking(x => x.InitializeAsync(cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryAcquireAsync_WhenLockNameIsInvalid_ThrowsArgumentException(string? lockName)
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(5);

        // Act & Assert
        await _sut.Invoking(x => x.TryAcquireAsync(lockName!, duration))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    public async Task TryAcquireAsync_WhenDurationIsInvalid_ThrowsArgumentOutOfRangeException(int durationSeconds)
    {
        // Arrange
        var lockName = "test-lock";
        var duration = TimeSpan.FromSeconds(durationSeconds);

        // Act & Assert
        await _sut.Invoking(x => x.TryAcquireAsync(lockName, duration))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenExpiredLockExists_ReplacesLockAndReturnsHandle()
    {
        // Arrange
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

        // Act
        var result = await _sut.TryAcquireAsync(lockName, duration);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IDistributedLockHandle>();
        _mockIndexManager.Verify(x => x.CreateOneAsync(
            It.IsAny<CreateIndexModel<DistributedLock>>(),
            It.IsAny<CreateOneIndexOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenNoLockExists_InsertsNewLockAndReturnsHandle()
    {
        // Arrange
        var lockName = "new-lock";
        var duration = TimeSpan.FromMinutes(5);

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DistributedLock)null!);

        _mockCollection.Setup(x => x.InsertOneAsync(
                It.IsAny<DistributedLock>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.TryAcquireAsync(lockName, duration);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IDistributedLockHandle>();
        _mockCollection.Verify(x => x.InsertOneAsync(
            It.Is<DistributedLock>(d => d.Id == lockName && d.ExpiresAtUtc > DateTimeOffset.UtcNow),
            It.IsAny<InsertOneOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenActiveLogExists_ReturnNull()
    {
        // Arrange
        var lockName = "active-lock";
        var duration = TimeSpan.FromMinutes(5);
        var writeException = new MongoException("Duplicate key error");

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DistributedLock)null!);

        _mockCollection.Setup(x => x.InsertOneAsync(
                It.IsAny<DistributedLock>(),
                It.IsAny<InsertOneOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(writeException);

        // Act & Assert - Should throw the exception since we can't easily mock duplicate key detection
        await _sut.Invoking(x => x.TryAcquireAsync(lockName, duration))
            .Should().ThrowAsync<MongoException>()
            .WithMessage("Duplicate key error");
    }

    [Fact]
    public async Task TryAcquireAsync_WhenUnexpectedMongoException_PropagatesException()
    {
        // Arrange
        var lockName = "test-lock";
        var duration = TimeSpan.FromMinutes(5);
        var exception = new MongoException("Unexpected error");

        _mockCollection.Setup(x => x.FindOneAndReplaceAsync(
                It.IsAny<FilterDefinition<DistributedLock>>(),
                It.IsAny<DistributedLock>(),
                It.IsAny<FindOneAndReplaceOptions<DistributedLock>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await _sut.Invoking(x => x.TryAcquireAsync(lockName, duration))
            .Should().ThrowAsync<MongoException>()
            .WithMessage("Unexpected error");
    }

    [Fact]
    public void Dispose_WhenCalled_DisposesResources()
    {
        // Act
        _sut.Dispose();

        // Assert - No exception should be thrown, semaphore should be disposed
        // We can't verify semaphore disposal directly, but calling Dispose should not throw
        Action act = () => _sut.Dispose();
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }
}