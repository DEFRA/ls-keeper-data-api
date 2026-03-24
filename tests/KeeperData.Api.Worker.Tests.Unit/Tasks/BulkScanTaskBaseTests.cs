using FluentAssertions;
using KeeperData.Api.Worker.Tasks.Implementations;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Documents;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeeperData.Api.Worker.Tests.Unit.Tasks;

public class BulkScanTaskBaseTests
{
    private readonly Mock<DataBridgeScanConfiguration> _configMock;
    private readonly Mock<IDistributedLock> _distributedLockMock;
    private readonly Mock<IHostApplicationLifetime> _applicationLifetimeMock;
    private readonly Mock<IDelayProvider> _delayProviderMock;
    private readonly Mock<IScanStateRepository> _scanStateRepositoryMock;
    private readonly Mock<IApplicationMetrics> _metricsMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TestBulkScanTask _sut;

    public BulkScanTaskBaseTests()
    {
        _configMock = new Mock<DataBridgeScanConfiguration>();
        _distributedLockMock = new Mock<IDistributedLock>();
        _applicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        _delayProviderMock = new Mock<IDelayProvider>();
        _scanStateRepositoryMock = new Mock<IScanStateRepository>();
        _metricsMock = new Mock<IApplicationMetrics>();
        _loggerMock = new Mock<ILogger>();

        _sut = new TestBulkScanTask(
            _configMock.Object,
            _distributedLockMock.Object,
            _applicationLifetimeMock.Object,
            _delayProviderMock.Object,
            _scanStateRepositoryMock.Object,
            _metricsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RecordScanStateAsync_ShouldCreateCorrectScanStateDocument()
    {
        // Arrange
        var scanCorrelationId = Guid.NewGuid();
        var scanStartedAt = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        var itemCount = 150;
        ScanStateDocument? capturedState = null;

        _scanStateRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ScanStateDocument, CancellationToken>((state, _) => capturedState = state)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.TestRecordScanStateAsync(scanCorrelationId, scanStartedAt, itemCount, CancellationToken.None);

        // Assert
        capturedState.Should().NotBeNull();
        capturedState!.Id.Should().Be("test-scan");
        capturedState.LastSuccessfulScanStartedAt.Should().Be(scanStartedAt);
        capturedState.LastSuccessfulScanCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        capturedState.LastScanCorrelationId.Should().Be(scanCorrelationId);
        capturedState.LastScanMode.Should().Be("bulk");
        capturedState.LastScanItemCount.Should().Be(itemCount);
    }

    [Fact]
    public async Task RecordScanStateAsync_ShouldCallRepositoryUpdateOnce()
    {
        // Arrange
        var scanCorrelationId = Guid.NewGuid();
        var scanStartedAt = DateTime.UtcNow;
        var itemCount = 100;

        _scanStateRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.TestRecordScanStateAsync(scanCorrelationId, scanStartedAt, itemCount, CancellationToken.None);

        // Assert
        _scanStateRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordScanStateAsync_WhenRepositoryThrows_ShouldNotPropagateException()
    {
        // Arrange
        var scanCorrelationId = Guid.NewGuid();
        var scanStartedAt = DateTime.UtcNow;
        var itemCount = 100;

        _scanStateRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var act = async () => await _sut.TestRecordScanStateAsync(
            scanCorrelationId, scanStartedAt, itemCount, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("scan state recording failure should be caught and logged");
    }

    [Fact]
    public async Task RecordScanStateAsync_WhenRepositoryThrows_ShouldLogError()
    {
        // Arrange
        var scanCorrelationId = Guid.NewGuid();
        var scanStartedAt = DateTime.UtcNow;
        var itemCount = 100;

        _scanStateRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        await _sut.TestRecordScanStateAsync(scanCorrelationId, scanStartedAt, itemCount, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to record scan state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordScanStateAsync_WithZeroItems_ShouldStillRecordState()
    {
        // Arrange
        var scanCorrelationId = Guid.NewGuid();
        var scanStartedAt = DateTime.UtcNow;
        var itemCount = 0;
        ScanStateDocument? capturedState = null;

        _scanStateRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ScanStateDocument, CancellationToken>((state, _) => capturedState = state)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.TestRecordScanStateAsync(scanCorrelationId, scanStartedAt, itemCount, CancellationToken.None);

        // Assert
        capturedState.Should().NotBeNull();
        capturedState!.LastScanItemCount.Should().Be(0);
        _scanStateRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordScanStateAsync_WithCancellationToken_ShouldPassToRepository()
    {
        // Arrange
        var scanCorrelationId = Guid.NewGuid();
        var scanStartedAt = DateTime.UtcNow;
        var itemCount = 100;
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _scanStateRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.TestRecordScanStateAsync(scanCorrelationId, scanStartedAt, itemCount, cancellationToken);

        // Assert
        _scanStateRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ScanStateDocument>(), cancellationToken),
            Times.Once);
    }

    // Test implementation to expose protected method
    private class TestBulkScanTask(
        DataBridgeScanConfiguration dataBridgeScanConfiguration,
        IDistributedLock distributedLock,
        IHostApplicationLifetime applicationLifetime,
        IDelayProvider delayProvider,
        IScanStateRepository scanStateRepository,
        IApplicationMetrics metrics,
        ILogger logger)
        : BulkScanTaskBase(dataBridgeScanConfiguration, distributedLock, applicationLifetime, delayProvider,
            scanStateRepository, metrics, logger)
    {
        protected override string ScanSourceId => "test-scan";
        protected override string LockName => "TestLock";

        protected override Task ExecuteTaskAsync(IDistributedLockHandle lockHandle, Guid scanCorrelationId, CancellationTokenSource linkedCts)
        {
            return Task.CompletedTask;
        }

        public Task TestRecordScanStateAsync(Guid scanCorrelationId, DateTime scanStartedAt, int itemCount, CancellationToken cancellationToken)
        {
            return RecordScanStateAsync(scanCorrelationId, scanStartedAt, "bulk", itemCount, cancellationToken);
        }
    }
}