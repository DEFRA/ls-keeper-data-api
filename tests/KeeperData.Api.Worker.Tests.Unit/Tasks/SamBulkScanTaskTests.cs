using FluentAssertions;
using KeeperData.Api.Worker.Tasks.Implementations;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KeeperData.Api.Worker.Tests.Unit.Tasks;

public class SamBulkScanTaskTests
{
    private readonly Mock<SamBulkScanOrchestrator> _orchestratorMock;
    private readonly DataBridgeScanConfiguration _config;
    private readonly Mock<IDistributedLock> _distributedLockMock;
    private readonly Mock<IHostApplicationLifetime> _lifetimeMock;
    private readonly Mock<ILogger<SamBulkScanTask>> _loggerMock;
    private readonly Mock<IDelayProvider> _delayProviderMock;
    private readonly SamBulkScanTask _sut;
    private readonly Mock<IDistributedLockHandle> _lockHandleMock;
    private readonly CancellationTokenSource _appStoppingCts;

    public SamBulkScanTaskTests()
    {
        _orchestratorMock = new Mock<SamBulkScanOrchestrator>(new List<Application.Orchestration.ChangeScanning.IScanStep<SamBulkScanContext>>());
        _config = new DataBridgeScanConfiguration { QueryPageSize = 100 };
        _distributedLockMock = new Mock<IDistributedLock>();
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _loggerMock = new Mock<ILogger<SamBulkScanTask>>();
        _delayProviderMock = new Mock<IDelayProvider>();
        _lockHandleMock = new Mock<IDistributedLockHandle>();
        _appStoppingCts = new CancellationTokenSource();

        _lifetimeMock.Setup(x => x.ApplicationStopping).Returns(_appStoppingCts.Token);

        _lockHandleMock.Setup(x => x.TryRenewAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _delayProviderMock.Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(async (TimeSpan _, CancellationToken token) => await Task.Delay(Timeout.Infinite, token));

        _sut = new SamBulkScanTask(
            _orchestratorMock.Object,
            _config,
            _distributedLockMock.Object,
            _lifetimeMock.Object,
            _delayProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task StartAsync_WhenLockAcquired_ShouldReturnCorrelationId()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var result = await _sut.StartAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        _loggerMock.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Lock acquired")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenApplicationStopping_ShouldLogWarning()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var orchestratorStarted = new TaskCompletionSource();
        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (SamBulkScanContext ctx, CancellationToken token) =>
            {
                orchestratorStarted.SetResult();
                try
                {
                    await Task.Delay(5000, token);
                }
                catch (TaskCanceledException)
                {
                    throw new OperationCanceledException();
                }
            });

        await _sut.StartAsync(CancellationToken.None);
        await orchestratorStarted.Task;

        _appStoppingCts.Cancel();
        await Task.Delay(100);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Application is shutting down")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenExceptionInBackgroundTask_ShouldLogError()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var expectedEx = new InvalidOperationException("Background failure");
        var orchestratorStarted = new TaskCompletionSource();

        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                orchestratorStarted.SetResult();
                await Task.Yield();
                throw expectedEx;
            });

        await _sut.StartAsync(CancellationToken.None);
        await orchestratorStarted.Task;
        await Task.Delay(100);

        _loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Background task failed")), expectedEx, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenLockNotAcquired_ShouldReturnNull()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDistributedLockHandle?)null);

        var result = await _sut.StartAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_WhenLockAcquired_ShouldExecuteOrchestratorAndDisposeLock()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        await _sut.RunAsync(CancellationToken.None);

        _orchestratorMock.Verify(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _lockHandleMock.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenLockNotAcquired_ShouldNotExecuteOrchestrator()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDistributedLockHandle?)null);

        await _sut.RunAsync(CancellationToken.None);

        _orchestratorMock.Verify(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenOrchestratorThrows_ShouldLogAndRethrow()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var expectedException = new InvalidOperationException("Fail");
        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        await _sut.Invoking(s => s.RunAsync(CancellationToken.None))
            .Should().ThrowAsync<Exception>();

        _lockHandleMock.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenLockRenewalFails_ShouldCancelImportAndThrow()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (SamBulkScanContext ctx, CancellationToken token) =>
            {
                try { await Task.Delay(Timeout.Infinite, token); }
                catch (TaskCanceledException) { throw new OperationCanceledException(); }
            });

        _delayProviderMock.Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _lockHandleMock.Setup(x => x.TryRenewAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.Invoking(s => s.RunAsync(CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Task was cancelled due to lock renewal failure");
    }

    [Fact]
    public async Task RunAsync_WhenRenewalCancelledDuringTryRenew_ShouldLogDebugAndExit()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var renewalAttempted = new TaskCompletionSource();

        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (SamBulkScanContext ctx, CancellationToken token) =>
            {
                await renewalAttempted.Task;
            });

        _delayProviderMock.Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _lockHandleMock.Setup(x => x.TryRenewAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback(() => renewalAttempted.SetResult())
            .ThrowsAsync(new OperationCanceledException());

        await _sut.RunAsync(CancellationToken.None);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Lock renewal cancelled")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenRenewalDelayCancelled_ShouldLogDebugAndExit()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var renewalStarted = new TaskCompletionSource();

        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (SamBulkScanContext ctx, CancellationToken token) =>
            {
                await renewalStarted.Task;
            });

        _delayProviderMock.Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback(() => renewalStarted.TrySetResult())
            .ThrowsAsync(new OperationCanceledException());

        await _sut.RunAsync(CancellationToken.None);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Lock renewal task cancelled")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenRenewalCancelledAfterDelay_ShouldExitLoop()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var cts = new CancellationTokenSource();
        var renewalHit = new TaskCompletionSource();

        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (SamBulkScanContext ctx, CancellationToken token) =>
            {
                try { await renewalHit.Task; }
                catch (TaskCanceledException) { }
                token.ThrowIfCancellationRequested();
            });

        _delayProviderMock.Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() =>
            {
                cts.Cancel();
                renewalHit.TrySetResult();
            });

        await _sut.Invoking(s => s.RunAsync(cts.Token))
             .Should().ThrowAsync<OperationCanceledException>();

        _lockHandleMock.Verify(x => x.TryRenewAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenRenewalTaskThrowsUnexpectedException_ShouldLogCriticalError()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var renewalHit = new TaskCompletionSource();

        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
             .Returns(async (SamBulkScanContext ctx, CancellationToken token) =>
             {
                 await renewalHit.Task;
             });

        var expectedEx = new InvalidOperationException("Unexpected error");

        _delayProviderMock.Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback(() => renewalHit.TrySetResult())
            .ThrowsAsync(expectedEx);

        await _sut.RunAsync(CancellationToken.None);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error in lock renewal task")),
            expectedEx,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldPeriodicallyRenewLock_AndLogSuccess()
    {
        _distributedLockMock.Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_lockHandleMock.Object);

        var renewalHappened = new TaskCompletionSource();

        _orchestratorMock.Setup(x => x.ExecuteAsync(It.IsAny<SamBulkScanContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (CtsBulkScanContext ctx, CancellationToken token) =>
            {
                await renewalHappened.Task;
            });

        _delayProviderMock.Setup(x => x.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _lockHandleMock.Setup(x => x.TryRenewAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(() => renewalHappened.TrySetResult());

        await _sut.RunAsync(CancellationToken.None);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully renewed lock")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);

        _lockHandleMock.Verify(x => x.DisposeAsync(), Times.Once);
    }
}