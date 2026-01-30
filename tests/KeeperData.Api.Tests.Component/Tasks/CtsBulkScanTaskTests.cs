using FluentAssertions;
using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Core.Exceptions;

namespace KeeperData.Api.Tests.Component.Tasks;

using KeeperData.Api.Worker.Tasks.Implementations;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
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

public class CtsBulkScanTaskTests
{
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = new()
    {
        QueryPageSize = 100,
        DelayBetweenQueriesSeconds = 0,
        LimitScanTotalBatchSize = 0,
        DailyScanIncludeChangesWithinTotalHours = 24
    };

    [Fact]
    public async Task RunAsync_Should_Execute_When_Lock_Acquired()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CtsBulkScanTask>>();
        var lockHandleMock = new Mock<IDistributedLockHandle>();
        var distributedLockMock = new Mock<IDistributedLock>();
        var delayProviderMock = new Mock<IDelayProvider>();
        var orchestrator = new CtsBulkScanOrchestrator([]);
        
        distributedLockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockHandleMock.Object);

        var appLifetimeMock = new Mock<IHostApplicationLifetime>();
        var task = new CtsBulkScanTask(
            orchestrator,
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
        var loggerMock = new Mock<ILogger<CtsBulkScanTask>>();
        var distributedLockMock = new Mock<IDistributedLock>();
        var delayProviderMock = new Mock<IDelayProvider>();
        var orchestrator = new CtsBulkScanOrchestrator([]);

        distributedLockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDistributedLockHandle?)null);

        var appLifetimeMock = new Mock<IHostApplicationLifetime>();
        var task = new CtsBulkScanTask(
            orchestrator,
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

    [Fact]
    public async Task RunAsync_ShouldBubbleException_WhenStepThrowsNonRetryableException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CtsBulkScanTask>>();
        var lockHandleMock = new Mock<IDistributedLockHandle>();
        var distributedLockMock = new Mock<IDistributedLock>();
        var delayProviderMock = new Mock<IDelayProvider>();
        var stepMock = new Mock<IScanStep<CtsBulkScanContext>>();
        var orchestrator = new CtsBulkScanOrchestrator([stepMock.Object]);
        
        distributedLockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockHandleMock.Object);
        
        stepMock
            .Setup(s => s.ExecuteAsync(It.IsAny<CtsBulkScanContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("Something went wrong"));

        var appLifetimeMock = new Mock<IHostApplicationLifetime>();
        var task = new CtsBulkScanTask(
            orchestrator,
            _dataBridgeScanConfiguration,
            distributedLockMock.Object,
            appLifetimeMock.Object,
            delayProviderMock.Object,
            loggerMock.Object);

        // Act
        Func<Task> act =  () => task.RunAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NonRetryableException>();
    }
    
    [Fact]
    public async Task RunAsync_ShouldBubbleException_WhenStepThrowsRetryableException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CtsBulkScanTask>>();
        var lockHandleMock = new Mock<IDistributedLockHandle>();
        var distributedLockMock = new Mock<IDistributedLock>();
        var delayProviderMock = new Mock<IDelayProvider>();
        var stepMock = new Mock<IScanStep<CtsBulkScanContext>>();
        var orchestrator = new CtsBulkScanOrchestrator([stepMock.Object]);
        
        distributedLockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockHandleMock.Object);
        
        stepMock
            .Setup(s => s.ExecuteAsync(It.IsAny<CtsBulkScanContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("Something went wrong"));

        var appLifetimeMock = new Mock<IHostApplicationLifetime>();
        var task = new CtsBulkScanTask(
            orchestrator,
            _dataBridgeScanConfiguration,
            distributedLockMock.Object,
            appLifetimeMock.Object,
            delayProviderMock.Object,
            loggerMock.Object);

        // Act
        Func<Task> act =  () => task.RunAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RetryableException>();
    }
}