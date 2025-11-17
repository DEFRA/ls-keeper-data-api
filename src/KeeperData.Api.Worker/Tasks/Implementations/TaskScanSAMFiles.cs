using KeeperData.Core.Locking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public class TaskScanSAMFiles(
    ILogger<TaskScanSAMFiles> logger,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime) : ITaskScanSAMFiles
{
    private const string LockName = nameof(TaskScanSAMFiles);
    private static readonly TimeSpan s_lockDuration = TimeSpan.FromMinutes(4);

    public async Task<Guid?> StartAsync(CancellationToken cancellationToken = default)
    {
        var scanCorrelationId = Guid.NewGuid();

        logger.LogInformation("Attempting to acquire lock for {LockName} with scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);

        var @lock = await distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);
            return null;
        }

        logger.LogInformation("Lock acquired for {LockName}. Task started at {startTime} scanCorrelationId: {scanCorrelationId}.", LockName, DateTime.UtcNow, scanCorrelationId);

        var stoppingToken = applicationLifetime.ApplicationStopping;

        _ = Task.Factory.StartNew(
            async () =>
            {
                try
                {
                    await using (@lock)
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                            cancellationToken,
                            stoppingToken);

                        await ExecuteTaskAsync(@lock, scanCorrelationId, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogWarning("Application is shutting down, task cancelled scanCorrelationId: {scanCorrelationId}", scanCorrelationId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background task failed scanCorrelationId: {scanCorrelationId}", scanCorrelationId);
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        ).Unwrap();

        return scanCorrelationId;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var scanCorrelationId = Guid.NewGuid();

        logger.LogInformation("Attempting to acquire lock for {LockName} scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);

        await using var @lock = await distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);
            return;
        }

        logger.LogInformation("Lock acquired for {LockName}. Task started at {startTime} scanCorrelationId: {scanCorrelationId}.", LockName, DateTime.UtcNow, scanCorrelationId);

        await ExecuteTaskAsync(@lock, scanCorrelationId, cancellationToken);
    }

    private async Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        CancellationToken externalCancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);

        // TODO: Add implementation in future story

        await Task.Delay(TimeSpan.FromSeconds(10), linkedCts.Token);
    }
}