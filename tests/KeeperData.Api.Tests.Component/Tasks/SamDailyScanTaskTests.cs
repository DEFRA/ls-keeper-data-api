namespace KeeperData.Api.Tests.Component.Tasks;

using KeeperData.Api.Worker.Tasks.Implementations;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class SamDailyScanTaskTests
{
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = new()
    {
        QueryPageSize = 100,
        DelayBetweenQueriesSeconds = 0,
        LimitScanTotalBatchSize = 0,
        DailyScanIncludeChangesWithinTotalHours = 24
    };

    private readonly SamDailyScanOrchestrator _orchestrator = new([]);

    [Fact]
    public async Task RunAsync_Should_Execute_When_Lock_Acquired()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SamDailyScanTask>>();
        var lockHandleMock = new Mock<IDistributedLockHandle>();
        var distributedLockMock = new Mock<IDistributedLock>();
        var delayProviderMock = new Mock<IDelayProvider>();

        distributedLockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockHandleMock.Object);

        var appLifetimeMock = new Mock<IHostApplicationLifetime>();
        var task = new SamDailyScanTask(
            _orchestrator,
            _dataBridgeScanConfiguration,
            distributedLockMock.Object,
            appLifetimeMock.Object,
            delayProviderMock.Object,
            loggerMock.Object);

        // Act
        await task.RunAsync(CancellationToken.None);

        // Assert
        distributedLockMock.Verify(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        lockHandleMock.Verify(l => l.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_Should_Not_Execute_When_Lock_Not_Acquired()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SamDailyScanTask>>();
        var distributedLockMock = new Mock<IDistributedLock>();
        var delayProviderMock = new Mock<IDelayProvider>();

        distributedLockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDistributedLockHandle?)null);

        var appLifetimeMock = new Mock<IHostApplicationLifetime>();
        var task = new SamDailyScanTask(
            _orchestrator,
            _dataBridgeScanConfiguration,
            distributedLockMock.Object,
            appLifetimeMock.Object,
            delayProviderMock.Object,
            loggerMock.Object);

        // Act
        await task.RunAsync(CancellationToken.None);

        // Assert
        distributedLockMock.Verify(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);

        loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString() != null && v.ToString()!.Contains("Could not acquire lock")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }
}