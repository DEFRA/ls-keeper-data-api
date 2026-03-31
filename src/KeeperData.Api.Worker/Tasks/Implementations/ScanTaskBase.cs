using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Documents;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public abstract class ScanTaskBase
{
    private static readonly TimeSpan s_lockDuration = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan s_renewalInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan s_renewalExtension = TimeSpan.FromMinutes(2);

    private readonly IDistributedLock _distributedLock;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IDelayProvider _delayProvider;

    protected DataBridgeScanConfiguration DataBridgeScanConfiguration { get; }
    protected IScanStateRepository ScanStateRepository { get; }
    protected IApplicationMetrics Metrics { get; }
    protected ILogger Logger { get; }
    protected abstract string LockName { get; }
    protected abstract string ScanSourceId { get; }

    protected ScanTaskBase(
        DataBridgeScanConfiguration dataBridgeScanConfiguration,
        IDistributedLock distributedLock,
        IHostApplicationLifetime applicationLifetime,
        IDelayProvider delayProvider,
        IScanStateRepository scanStateRepository,
        IApplicationMetrics metrics,
        ILogger logger)
    {
        DataBridgeScanConfiguration = dataBridgeScanConfiguration;
        _distributedLock = distributedLock;
        _applicationLifetime = applicationLifetime;
        _delayProvider = delayProvider;
        ScanStateRepository = scanStateRepository;
        Metrics = metrics;
        Logger = logger;
    }

    protected async Task<Guid?> StartScanAsync(
        Func<IDistributedLockHandle, Guid, CancellationTokenSource, Task> executeFunc,
        CancellationToken cancellationToken = default)
    {
        var scanCorrelationId = Guid.NewGuid();

        Logger.LogInformation("Attempting to acquire lock for {LockName} with scanCorrelationId: {ScanCorrelationId}.", LockName, scanCorrelationId);

        var @lock = await _distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            Logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running scanCorrelationId: {ScanCorrelationId}.", LockName, scanCorrelationId);
            return null;
        }

        Logger.LogInformation("Lock acquired for {LockName}. Task started at {StartTime} scanCorrelationId: {ScanCorrelationId}.", LockName, DateTime.UtcNow, scanCorrelationId);

        var stoppingToken = _applicationLifetime.ApplicationStopping;

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

                        await executeFunc(@lock, scanCorrelationId, cts);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    Logger.LogWarning("Application is shutting down, task cancelled scanCorrelationId: {ScanCorrelationId}", scanCorrelationId);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Background task failed scanCorrelationId: {ScanCorrelationId}", scanCorrelationId);
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        ).Unwrap();

        return scanCorrelationId;
    }

    protected async Task RunScanAsync(
        Func<IDistributedLockHandle, Guid, CancellationTokenSource, Task> executeFunc,
        CancellationToken cancellationToken)
    {
        var scanCorrelationId = Guid.NewGuid();

        Logger.LogInformation("Attempting to acquire lock for {LockName} scanCorrelationId: {ScanCorrelationId}.", LockName, scanCorrelationId);

        await using var @lock = await _distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            Logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running scanCorrelationId: {ScanCorrelationId}.", LockName, scanCorrelationId);
            return;
        }

        Logger.LogInformation("Lock acquired for {LockName}. Task started at {StartTime} scanCorrelationId: {ScanCorrelationId}.", LockName, DateTime.UtcNow, scanCorrelationId);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await executeFunc(@lock, scanCorrelationId, cts);
    }

    protected async Task RecordScanStateAsync(
        Guid scanCorrelationId,
        DateTime scanStartedAt,
        string scanMode,
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
                LastScanMode = scanMode,
                LastScanItemCount = itemCount
            };

            await ScanStateRepository.UpdateAsync(scanState, cancellationToken);

            Logger.LogInformation(
                "Recorded scan state for {ScanSourceId}: mode={ScanMode}, items={ItemCount}, scanCorrelationId: {ScanCorrelationId}",
                ScanSourceId, scanMode, itemCount, scanCorrelationId);
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
        Logger.LogDebug("Starting lock renewal task for {LockName} with interval {RenewalInterval} scanCorrelationId: {ScanCorrelationId}", LockName, s_renewalInterval, scanCorrelationId);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await _delayProvider.DelayAsync(s_renewalInterval, token);
            }
            catch (OperationCanceledException)
            {
                Logger.LogDebug("Lock renewal task cancelled for {LockName} scanCorrelationId: {ScanCorrelationId}", LockName, scanCorrelationId);
                return;
            }

            if (token.IsCancellationRequested) return;

            Logger.LogDebug("Attempting to renew lock for {LockName} scanCorrelationId: {ScanCorrelationId}", LockName, scanCorrelationId);

            bool renewed;
            try
            {
                renewed = await lockHandle.TryRenewAsync(s_renewalExtension, token);
            }
            catch (OperationCanceledException)
            {
                Logger.LogDebug("Lock renewal cancelled for {LockName} scanCorrelationId: {ScanCorrelationId}", LockName, scanCorrelationId);
                return;
            }

            if (renewed)
            {
                Logger.LogDebug("Successfully renewed lock for {LockName} with extension {RenewalExtension} scanCorrelationId: {ScanCorrelationId}", LockName, s_renewalExtension, scanCorrelationId);
            }
            else
            {
                Logger.LogError("Failed to renew lock for {LockName}. Lock may have been lost. Cancelling main task. scanCorrelationId: {ScanCorrelationId}", LockName, scanCorrelationId);
                await linkedCts.CancelAsync();
                throw new InvalidOperationException($"Failed to renew lock for {LockName} scanCorrelationId: {scanCorrelationId}");
            }
        }

        Logger.LogDebug("Lock renewal task completed for {LockName} scanCorrelationId: {ScanCorrelationId}", LockName, scanCorrelationId);
    }
}