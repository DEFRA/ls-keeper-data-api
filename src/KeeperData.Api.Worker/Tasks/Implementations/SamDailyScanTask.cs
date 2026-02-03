using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public class SamDailyScanTask(
    SamDailyScanOrchestrator orchestrator,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    IDelayProvider delayProvider,
    ILogger<SamDailyScanTask> logger) : ISamDailyScanTask
{
    private const string LockName = nameof(SamDailyScanTask);
    private static readonly TimeSpan s_lockDuration = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan s_renewalInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan s_renewalExtension = TimeSpan.FromMinutes(2);

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

                        await ExecuteTaskAsync(@lock, scanCorrelationId, cts);
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

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await ExecuteTaskAsync(@lock, scanCorrelationId, cts);
    }

    private async Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        CancellationTokenSource linkedCts)
    {
        var externalToken = linkedCts.Token;
        var renewalTask = RenewLockPeriodicallyAsync(lockHandle, scanCorrelationId, linkedCts);

        try
        {
            var context = new SamDailyScanContext
            {
                ScanCorrelationId = scanCorrelationId,
                CurrentDateTime = DateTime.UtcNow,
                UpdatedSinceDateTime = DateTime.UtcNow.AddHours(dataBridgeScanConfiguration.DailyScanIncludeChangesWithinTotalHours),
                PageSize = dataBridgeScanConfiguration.QueryPageSize,
                Holdings = new(),
                Holders = new(),
                Herds = new(),
                Parties = new()
            };

            await orchestrator.ExecuteAsync(context, linkedCts.Token);

            logger.LogInformation("Import completed successfully at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
        }
        catch (OperationCanceledException)
        {
            if (renewalTask.IsFaulted || (renewalTask.IsCompleted && !externalToken.IsCancellationRequested))
            {
                logger.LogError("Import was stopped due to lock renewal failure at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
                throw new InvalidOperationException("Task was cancelled due to lock renewal failure");
            }

            if (externalToken.IsCancellationRequested)
            {
                logger.LogInformation("Import was cancelled at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
                throw;
            }
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during import execution scanCorrelationId: {scanCorrelationId}", scanCorrelationId);
            throw;
        }
        finally
        {
            if (!linkedCts.IsCancellationRequested)
            {
                await linkedCts.CancelAsync();
            }

            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException) {}
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in lock renewal task for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
            }
        }
    }

    private async Task RenewLockPeriodicallyAsync(IDistributedLockHandle lockHandle, Guid scanCorrelationId, CancellationTokenSource linkedCts)
    {
        var token = linkedCts.Token;
        logger.LogDebug("Starting lock renewal task for {LockName} with interval {RenewalInterval} scanCorrelationId: {scanCorrelationId}", LockName, s_renewalInterval, scanCorrelationId);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await delayProvider.DelayAsync(s_renewalInterval, token);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Lock renewal task cancelled for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
                return;
            }

            if (token.IsCancellationRequested) return;

            logger.LogDebug("Attempting to renew lock for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);

            bool renewed;
            try
            {
                renewed = await lockHandle.TryRenewAsync(s_renewalExtension, token);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Lock renewal cancelled for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
                return;
            }

            if (renewed)
            {
                logger.LogDebug("Successfully renewed lock for {LockName} with extension {RenewalExtension} scanCorrelationId: {scanCorrelationId}", LockName, s_renewalExtension, scanCorrelationId);
            }
            else
            {
                logger.LogError("Failed to renew lock for {LockName}. Lock may have been lost. Cancelling main task. scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
                await linkedCts.CancelAsync();
                throw new InvalidOperationException($"Failed to renew lock for {LockName} scanCorrelationId: {scanCorrelationId}");
            }
        }

        logger.LogDebug("Lock renewal task completed for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
    }
}