using KeeperData.Core.Locking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public class TaskScanSAMBulkFiles(
    ILogger<TaskScanSAMBulkFiles> logger,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime) : ITaskScanSAMBulkFiles
{
    private const string LockName = nameof(TaskScanSAMBulkFiles);
    private static readonly TimeSpan s_lockDuration = TimeSpan.FromMinutes(4);

    public async Task<Guid?> StartAsync(string sourceType, CancellationToken cancellationToken = default)
    {
        var scanId = Guid.NewGuid();
        logger.LogInformation("StartAsync called for {LockName} with sourceType={sourceType} (scanId={scanId})", LockName, sourceType, scanId);

        logger.LogInformation("Attempting to acquire lock for {LockName} with sourceType={sourceType} (scanId={scanId}).", LockName, sourceType, scanId);

        var @lock = await distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running (scanId={scanId}).", LockName, scanId);
            logger.LogInformation("StartAsync exiting early for {LockName} (scanId={scanId})", LockName, scanId);
            return null;
        }

        logger.LogInformation("Lock acquired for {LockName}. Starting scan in background with sourceType={sourceType} (scanId={scanId}).", LockName, sourceType, scanId);

        var stoppingToken = applicationLifetime.ApplicationStopping;

        _ = Task.Factory.StartNew(
            async () =>
            {
                logger.LogInformation("Background scan started for {LockName} (scanId={scanId})", LockName, scanId);
                try
                {
                    await using (@lock)
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                            cancellationToken,
                            stoppingToken);

                        // TODO: Add implementation in future story
                        return;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogWarning("Application is shutting down, scan cancelled (scanId={scanId})", scanId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background scan failed (scanId={scanId})", scanId);
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        ).Unwrap();

        logger.LogInformation("StartAsync completed for {LockName} (scanId={scanId})", LockName, scanId);
        return scanId;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var scanId = Guid.NewGuid();
        logger.LogInformation("RunAsync called for {LockName} (scanId={scanId})", LockName, scanId);

        logger.LogInformation("Attempting to acquire lock for {LockName} (scanId={scanId}).", LockName, scanId);

        await using var @lock = await distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running  (scanId={scanId}).", LockName, scanId);
            logger.LogInformation("RunAsync exiting early for {LockName} (scanId={scanId})", LockName, scanId);
            return;
        }

        logger.LogInformation("Lock acquired for {LockName}. Task started at {startTime} (scanId={scanId}).", LockName, DateTime.UtcNow, scanId);

        // TODO: Add implementation in future story
        return;
    }
}