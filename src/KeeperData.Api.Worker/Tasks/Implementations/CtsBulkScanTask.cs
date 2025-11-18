using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public class CtsBulkScanTask(
    CtsBulkScanOrchestrator orchestrator,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    ILogger<CtsBulkScanTask> logger) : ICtsBulkScanTask
{
    private const string LockName = nameof(CtsBulkScanTask);
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

        var renewalTask = RenewLockPeriodicallyAsync(lockHandle, linkedCts.Token, scanCorrelationId);

        try
        {
            var context = new CtsBulkScanContext
            {
                ScanCorrelationId = scanCorrelationId,
                CurrentDateTime = DateTime.UtcNow,
                UpdatedSinceDateTime = null,
                PageSize = dataBridgeScanConfiguration.QueryPageSize,
                Holdings = new()
            };

            await orchestrator.ExecuteAsync(context, linkedCts.Token);

            logger.LogInformation("Import completed successfully at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
        }
        catch (OperationCanceledException) when (externalCancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Import was cancelled at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
            throw;
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !externalCancellationToken.IsCancellationRequested)
        {
            logger.LogError("Import was stopped due to lock renewal failure at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
            throw new InvalidOperationException("Task was cancelled due to lock renewal failure");
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
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in lock renewal task for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
            }
        }
    }

    private async Task RenewLockPeriodicallyAsync(IDistributedLockHandle lockHandle, CancellationToken cancellationToken, Guid scanCorrelationId)
    {
        logger.LogDebug("Starting lock renewal task for {LockName} with interval {RenewalInterval} scanCorrelationId: {scanCorrelationId}", LockName, s_renewalInterval, scanCorrelationId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(s_renewalInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Lock renewal task cancelled for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            logger.LogDebug("Attempting to renew lock for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);

            bool renewed;
            try
            {
                renewed = await lockHandle.TryRenewAsync(s_renewalExtension, cancellationToken);
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

                throw new InvalidOperationException($"Failed to renew lock for {LockName} scanCorrelationId: {scanCorrelationId}");
            }
        }

        logger.LogDebug("Lock renewal task completed for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
    }
}