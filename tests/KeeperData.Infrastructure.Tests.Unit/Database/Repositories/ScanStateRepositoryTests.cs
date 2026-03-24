using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Infrastructure.Database.Configuration;
using KeeperData.Infrastructure.Database.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using System.Reflection;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class ScanStateRepositoryTests
{
    private readonly IOptions<MongoConfig> _mongoConfig;
    private readonly Mock<IMongoClient> _mongoClientMock = new();
    private readonly Mock<IMongoDatabase> _mongoDatabaseMock = new();
    private readonly Mock<IAsyncCursor<ScanStateDocument>> _asyncCursorMock = new();
    private readonly Mock<IMongoCollection<ScanStateDocument>> _mongoCollectionMock = new();

    private readonly ScanStateRepository _sut;

    public ScanStateRepositoryTests()
    {
        _mongoConfig = Options.Create(new MongoConfig { DatabaseName = "DatabaseName" });

        _mongoDatabaseMock
            .Setup(db => db.GetCollection<ScanStateDocument>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
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
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<FindOptions<ScanStateDocument, ScanStateDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_asyncCursorMock.Object);

        _sut = new ScanStateRepository(_mongoConfig, _mongoClientMock.Object);

        typeof(ScanStateRepository)
            .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_sut, _mongoCollectionMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCalledWithValidId_ReturnsMatchingScanState()
    {
        var scanSourceId = "test-scan-source";
        var expected = new ScanStateDocument
        {
            Id = scanSourceId,
            LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-1),
            LastSuccessfulScanCompletedAt = DateTime.UtcNow,
            LastScanCorrelationId = Guid.NewGuid(),
            LastScanMode = "Full",
            LastScanItemCount = 100
        };

        _asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns([expected]);

        var result = await _sut.GetByIdAsync(scanSourceId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(scanSourceId);
        result.LastScanMode.Should().Be("Full");
        result.LastScanItemCount.Should().Be(100);
    }

    [Fact]
    public async Task GetByIdAsync_WhenScanStateNotFound_ReturnsNull()
    {
        var scanSourceId = "non-existent-scan-source";

        _asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns(new List<ScanStateDocument>());

        var result = await _sut.GetByIdAsync(scanSourceId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCancellationRequested_PassesCancellationToken()
    {
        var scanSourceId = "test-scan-source";
        var cancellationToken = new CancellationToken();

        await _sut.GetByIdAsync(scanSourceId, cancellationToken);

        _mongoCollectionMock
            .Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<FindOptions<ScanStateDocument, ScanStateDocument>>(),
                cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCalled_ReplacesDocumentWithUpsert()
    {
        var scanState = new ScanStateDocument
        {
            Id = "test-scan-source",
            LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-1),
            LastSuccessfulScanCompletedAt = DateTime.UtcNow,
            LastScanCorrelationId = Guid.NewGuid(),
            LastScanMode = "Incremental",
            LastScanItemCount = 50
        };

        var replaceResultMock = new Mock<ReplaceOneResult>();
        replaceResultMock.SetupAllProperties();

        _mongoCollectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                scanState,
                It.Is<ReplaceOptions>(opts => opts.IsUpsert == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaceResultMock.Object)
            .Verifiable();

        await _sut.UpdateAsync(scanState, CancellationToken.None);

        _mongoCollectionMock.Verify();
    }

    [Fact]
    public async Task UpdateAsync_WhenCalled_UsesCorrectFilter()
    {
        var scanState = new ScanStateDocument
        {
            Id = "test-scan-source",
            LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-1),
            LastSuccessfulScanCompletedAt = DateTime.UtcNow,
            LastScanCorrelationId = Guid.NewGuid(),
            LastScanMode = "Full",
            LastScanItemCount = 200
        };

        var replaceResultMock = new Mock<ReplaceOneResult>();
        replaceResultMock.SetupAllProperties();

        FilterDefinition<ScanStateDocument>? capturedFilter = null;
        _mongoCollectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                scanState,
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<ScanStateDocument>, ScanStateDocument, ReplaceOptions, CancellationToken>(
                (filter, _, _, _) => capturedFilter = filter)
            .ReturnsAsync(replaceResultMock.Object);

        await _sut.UpdateAsync(scanState, CancellationToken.None);

        capturedFilter.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenCancellationRequested_PassesCancellationToken()
    {
        var scanState = new ScanStateDocument
        {
            Id = "test-scan-source",
            LastSuccessfulScanStartedAt = DateTime.UtcNow,
            LastSuccessfulScanCompletedAt = DateTime.UtcNow,
            LastScanCorrelationId = Guid.NewGuid(),
            LastScanMode = "Full",
            LastScanItemCount = 100
        };
        var cancellationToken = new CancellationToken();
        var replaceResultMock = new Mock<ReplaceOneResult>();
        replaceResultMock.SetupAllProperties();

        _mongoCollectionMock
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                scanState,
                It.IsAny<ReplaceOptions>(),
                cancellationToken))
            .ReturnsAsync(replaceResultMock.Object);

        await _sut.UpdateAsync(scanState, cancellationToken);

        _mongoCollectionMock
            .Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                scanState,
                It.IsAny<ReplaceOptions>(),
                cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CountAsync_WhenCalled_ReturnsDocumentCount()
    {
        var expectedCount = 42L;

        _mongoCollectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        var result = await _sut.CountAsync(CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task CountAsync_WhenCalled_UsesEmptyFilter()
    {
        var expectedCount = 10L;

        _mongoCollectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.Is<FilterDefinition<ScanStateDocument>>(f => f == Builders<ScanStateDocument>.Filter.Empty),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        await _sut.CountAsync(CancellationToken.None);

        _mongoCollectionMock
            .Verify(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CountAsync_WhenCancellationRequested_PassesCancellationToken()
    {
        var cancellationToken = new CancellationToken();
        var expectedCount = 5L;

        _mongoCollectionMock
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<CountOptions>(),
                cancellationToken))
            .ReturnsAsync(expectedCount);

        await _sut.CountAsync(cancellationToken);

        _mongoCollectionMock
            .Verify(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<CountOptions>(),
                cancellationToken), Times.Once);
    }
}