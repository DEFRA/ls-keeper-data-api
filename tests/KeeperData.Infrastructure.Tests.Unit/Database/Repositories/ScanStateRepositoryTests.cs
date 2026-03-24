using FluentAssertions;
using KeeperData.Core.Documents;
using KeeperData.Infrastructure.Database.Repositories;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Database.Repositories;

public class ScanStateRepositoryTests
{
    private readonly MockMongoDatabase _mockDb = new();
    private readonly ScanStateRepository _sut;

    public ScanStateRepositoryTests()
    {
        _mockDb.SetupCollection<ScanStateDocument>();
        _sut = new ScanStateRepository(_mockDb.Config, _mockDb.Client);
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

        var asyncCursorMock = new Mock<IAsyncCursor<ScanStateDocument>>();
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns([expected]);

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<FindOptions<ScanStateDocument, ScanStateDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asyncCursorMock.Object);

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

        var asyncCursorMock = new Mock<IAsyncCursor<ScanStateDocument>>();
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns(new List<ScanStateDocument>());

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<FindOptions<ScanStateDocument, ScanStateDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(asyncCursorMock.Object);

        var result = await _sut.GetByIdAsync(scanSourceId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCancellationRequested_PassesCancellationToken()
    {
        var scanSourceId = "test-scan-source";
        var cancellationToken = new CancellationToken();

        var asyncCursorMock = new Mock<IAsyncCursor<ScanStateDocument>>();
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(cancellationToken))
            .ReturnsAsync(false);

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<FindOptions<ScanStateDocument, ScanStateDocument>>(),
                cancellationToken))
            .ReturnsAsync(asyncCursorMock.Object);

        await _sut.GetByIdAsync(scanSourceId, cancellationToken);

        _mockDb.MockCollection<ScanStateDocument>()
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

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                scanState,
                It.Is<ReplaceOptions>(opts => opts.IsUpsert == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaceResultMock.Object)
            .Verifiable();

        await _sut.UpdateAsync(scanState, CancellationToken.None);

        _mockDb.MockCollection<ScanStateDocument>().Verify();
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
        _mockDb.MockCollection<ScanStateDocument>()
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

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                scanState,
                It.IsAny<ReplaceOptions>(),
                cancellationToken))
            .ReturnsAsync(replaceResultMock.Object);

        await _sut.UpdateAsync(scanState, cancellationToken);

        _mockDb.MockCollection<ScanStateDocument>()
            .Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                scanState,
                It.IsAny<ReplaceOptions>(),
                cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsSortedPaginatedResults()
    {
        var scanStates = new List<ScanStateDocument>
        {
            new()
            {
                Id = "scan-1",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-2),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow.AddHours(-1),
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Full",
                LastScanItemCount = 100
            },
            new()
            {
                Id = "scan-2",
                LastSuccessfulScanStartedAt = DateTime.UtcNow.AddHours(-1),
                LastSuccessfulScanCompletedAt = DateTime.UtcNow,
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Incremental",
                LastScanItemCount = 50
            }
        };

        var asyncCursorMock = new Mock<IAsyncCursor<ScanStateDocument>>();
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns(scanStates);

        var fluentFindMock = SetupFluentFind(asyncCursorMock.Object);

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.Find(It.IsAny<FilterDefinition<ScanStateDocument>>(), It.IsAny<FindOptions>()))
            .Returns(fluentFindMock.Object);

        var result = await _sut.GetAllAsync(0, 10, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_AppliesSkipAndLimit()
    {
        var scanStates = new List<ScanStateDocument>
        {
            new()
            {
                Id = "scan-1",
                LastSuccessfulScanStartedAt = DateTime.UtcNow,
                LastSuccessfulScanCompletedAt = DateTime.UtcNow,
                LastScanCorrelationId = Guid.NewGuid(),
                LastScanMode = "Full",
                LastScanItemCount = 100
            }
        };

        var asyncCursorMock = new Mock<IAsyncCursor<ScanStateDocument>>();
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns(scanStates);

        var fluentFindMock = SetupFluentFind(asyncCursorMock.Object);

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.Find(It.IsAny<FilterDefinition<ScanStateDocument>>(), It.IsAny<FindOptions>()))
            .Returns(fluentFindMock.Object);

        await _sut.GetAllAsync(5, 20, CancellationToken.None);

        fluentFindMock.Verify(f => f.Skip(5), Times.Once);
        fluentFindMock.Verify(f => f.Limit(20), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_SortsByLastSuccessfulScanCompletedAtDescending()
    {
        var scanStates = new List<ScanStateDocument>();

        var asyncCursorMock = new Mock<IAsyncCursor<ScanStateDocument>>();
        asyncCursorMock
            .SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        asyncCursorMock
            .SetupGet(c => c.Current)
            .Returns(scanStates);

        var fluentFindMock = SetupFluentFind(asyncCursorMock.Object);

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.Find(It.IsAny<FilterDefinition<ScanStateDocument>>(), It.IsAny<FindOptions>()))
            .Returns(fluentFindMock.Object);

        await _sut.GetAllAsync(0, 10, CancellationToken.None);

        fluentFindMock.Verify(f => f.Sort(It.IsAny<SortDefinition<ScanStateDocument>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCancellationRequested_PassesCancellationToken()
    {
        var cancellationToken = new CancellationToken();
        var asyncCursorMock = new Mock<IAsyncCursor<ScanStateDocument>>();
        asyncCursorMock
            .Setup(c => c.MoveNextAsync(cancellationToken))
            .ReturnsAsync(false);

        var fluentFindMock = SetupFluentFind(asyncCursorMock.Object);

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.Find(It.IsAny<FilterDefinition<ScanStateDocument>>(), It.IsAny<FindOptions>()))
            .Returns(fluentFindMock.Object);

        await _sut.GetAllAsync(0, 10, cancellationToken);

        fluentFindMock.Verify(f => f.ToCursorAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CountAsync_WhenCalled_ReturnsDocumentCount()
    {
        var expectedCount = 42L;

        _mockDb.MockCollection<ScanStateDocument>()
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

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.CountDocumentsAsync(
                It.Is<FilterDefinition<ScanStateDocument>>(f => f == Builders<ScanStateDocument>.Filter.Empty),
                It.IsAny<CountOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        await _sut.CountAsync(CancellationToken.None);

        _mockDb.MockCollection<ScanStateDocument>()
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

        _mockDb.MockCollection<ScanStateDocument>()
            .Setup(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<CountOptions>(),
                cancellationToken))
            .ReturnsAsync(expectedCount);

        await _sut.CountAsync(cancellationToken);

        _mockDb.MockCollection<ScanStateDocument>()
            .Verify(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<ScanStateDocument>>(),
                It.IsAny<CountOptions>(),
                cancellationToken), Times.Once);
    }

    private Mock<IFindFluent<ScanStateDocument, ScanStateDocument>> SetupFluentFind(IAsyncCursor<ScanStateDocument> cursor)
    {
        var fluentFindMock = new Mock<IFindFluent<ScanStateDocument, ScanStateDocument>>();
        fluentFindMock
            .Setup(f => f.Sort(It.IsAny<SortDefinition<ScanStateDocument>>()))
            .Returns(fluentFindMock.Object);
        fluentFindMock
            .Setup(f => f.Skip(It.IsAny<int>()))
            .Returns(fluentFindMock.Object);
        fluentFindMock
            .Setup(f => f.Limit(It.IsAny<int>()))
            .Returns(fluentFindMock.Object);
        fluentFindMock
            .Setup(f => f.ToCursorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursor);
        return fluentFindMock;
    }
}