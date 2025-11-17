namespace KeeperData.Api.Tests.Component.Tasks;

using KeeperData.Api.Worker.Tasks.Implementations;
using KeeperData.Core.Locking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class TaskScanCTSBulkFilesTests
{
    [Fact]
    public async Task RunAsync_Should_Execute_When_Lock_Acquired()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TaskScanCTSBulkFiles>>();
        var lockHandleMock = new Mock<IDistributedLockHandle>();
        var distributedLockMock = new Mock<IDistributedLock>();
        distributedLockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockHandleMock.Object);

        var appLifetimeMock = new Mock<IHostApplicationLifetime>();
        var task = new TaskScanCTSBulkFiles(loggerMock.Object, distributedLockMock.Object, appLifetimeMock.Object);

        // Act
        await task.RunAsync(CancellationToken.None);

        // Assert
        distributedLockMock.Verify(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        lockHandleMock.Verify(l => l.DisposeAsync(), Times.Once); // Ensures lock is disposed after use
    }

    [Fact]
    public async Task RunAsync_Should_Not_Execute_When_Lock_Not_Acquired()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TaskScanCTSBulkFiles>>();
        var distributedLockMock = new Mock<IDistributedLock>();
        distributedLockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDistributedLockHandle?)null);

        var appLifetimeMock = new Mock<IHostApplicationLifetime>();
        var task = new TaskScanCTSBulkFiles(loggerMock.Object, distributedLockMock.Object, appLifetimeMock.Object);

        // Act
        await task.RunAsync(CancellationToken.None);

        // Assert
        distributedLockMock.Verify(l => l.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);

        // No further action should be taken if lock is not acquired
        loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString() != null && v.ToString()!.Contains("Could not acquire lock")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }
}