using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Documents;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public abstract class BulkScanTaskBase(
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    IDelayProvider delayProvider,
    IScanStateRepository scanStateRepository,
    IApplicationMetrics metrics,
    ILogger logger)
    : IScanTask
{
    private static readonly TimeSpan s_lockDuration = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan s_renewalInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan s_renewalExtension = TimeSpan.FromMinutes(2);

    protected DataBridgeScanConfiguration DataBridgeScanConfiguration { get; } = dataBridgeScanConfiguration;
    protected IScanStateRepository ScanStateRepository { get; } = scanStateRepository;
    protected IApplicationMetrics Metrics { get; } = metrics;
    protected ILogger Logger { get; } = logger;
    protected abstract string LockName { get; }
    protected abstract string ScanSourceId { get; }

    public async Task<Guid?> StartAsync(CancellationToken cancellationToken = default)
    {
        var scanCorrelationId = Guid.NewGuid();

        Logger.LogInformation("Attempting to acquire lock for {LockName} with scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);

        var @lock = await distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            Logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);
            return null;
        }

        Logger.LogInformation("Lock acquired for {LockName}. Task started at {startTime} scanCorrelationId: {scanCorrelationId}.", LockName, DateTime.UtcNow, scanCorrelationId);

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
                    Logger.LogWarning("Application is shutting down, task cancelled scanCorrelationId: {scanCorrelationId}", scanCorrelationId);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Background task failed scanCorrelationId: {scanCorrelationId}", scanCorrelationId);
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

        Logger.LogInformation("Attempting to acquire lock for {LockName} scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);

        await using var @lock = await distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            Logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);
            return;
        }

        Logger.LogInformation("Lock acquired for {LockName}. Task started at {startTime} scanCorrelationId: {scanCorrelationId}.", LockName, DateTime.UtcNow, scanCorrelationId);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await ExecuteTaskAsync(@lock, scanCorrelationId, cts);
    }

    protected abstract Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        CancellationTokenSource linkedCts);

    protected async Task RecordScanStateAsync(
        Guid scanCorrelationId,
        DateTime scanStartedAt,
        int itemCount,
        CancellationToken cancellationToken)
    {
        try
        {
            var scanState = new ScanStateDocument
            {
                Id = ScanSourceId,
                LastSuccessfulScanStartedAt = scanStartedAt,
                LastSuccessfulScanCompletedAt = DateTime.UtcNow,
                LastScanCorrelationId = scanCorrelationId,
                LastScanMode = "bulk",
                LastScanItemCount = itemCount
            };

            await ScanStateRepository.UpdateAsync(scanState, cancellationToken);

            Logger.LogInformation(
                "Recorded scan state for {ScanSourceId}: mode=bulk, items={ItemCount}, scanCorrelationId: {ScanCorrelationId}",
                ScanSourceId, itemCount, scanCorrelationId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Failed to record scan state for {ScanSourceId}, scanCorrelationId: {ScanCorrelationId}. " +
                "Scan completed successfully but state was not persisted.",
                ScanSourceId, scanCorrelationId);
        }
    }

    protected async Task RenewLockPeriodicallyAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        CancellationTokenSource linkedCts)
    {
        var token = linkedCts.Token;
        Logger.LogDebug("Starting lock renewal task for {LockName} with interval {RenewalInterval} scanCorrelationId: {scanCorrelationId}", LockName, s_renewalInterval, scanCorrelationId);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await delayProvider.DelayAsync(s_renewalInterval, token);
            }
            catch (OperationCanceledException)
            {
                Logger.LogDebug("Lock renewal task cancelled for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
                return;
            }

            if (token.IsCancellationRequested) return;

            Logger.LogDebug("Attempting to renew lock for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);

            bool renewed;
            try
            {
                renewed = await lockHandle.TryRenewAsync(s_renewalExtension, token);
            }
            catch (OperationCanceledException)
            {
                Logger.LogDebug("Lock renewal cancelled for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
                return;
            }

            if (renewed)
            {
                Logger.LogDebug("Successfully renewed lock for {LockName} with extension {RenewalExtension} scanCorrelationId: {scanCorrelationId}", LockName, s_renewalExtension, scanCorrelationId);
            }
            else
            {
                Logger.LogError("Failed to renew lock for {LockName}. Lock may have been lost. Cancelling main task. scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
                await linkedCts.CancelAsync();
                throw new InvalidOperationException($"Failed to renew lock for {LockName} scanCorrelationId: {scanCorrelationId}");
            }
        }

        Logger.LogDebug("Lock renewal task completed for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
    }
}