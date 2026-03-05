using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public abstract class DailyScanTaskBase : IDailyScanTask
{
    private static readonly TimeSpan s_lockDuration = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan s_renewalInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan s_renewalExtension = TimeSpan.FromMinutes(2);

    private readonly IDistributedLock _distributedLock;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IDelayProvider _delayProvider;

    protected DataBridgeScanConfiguration DataBridgeScanConfiguration { get; }
    protected IApplicationMetrics Metrics { get; }
    protected ILogger Logger { get; }
    protected abstract string LockName { get; }

    protected DailyScanTaskBase(
        DataBridgeScanConfiguration dataBridgeScanConfiguration,
        IDistributedLock distributedLock,
        IHostApplicationLifetime applicationLifetime,
        IDelayProvider delayProvider,
        IApplicationMetrics metrics,
        ILogger logger)
    {
        DataBridgeScanConfiguration = dataBridgeScanConfiguration;
        _distributedLock = distributedLock;
        _applicationLifetime = applicationLifetime;
        _delayProvider = delayProvider;
        Metrics = metrics;
        Logger = logger;
    }

    public async Task<Guid?> StartAsync(CancellationToken cancellationToken = default) =>
        await SharedStartAsync(null, cancellationToken);

    public async Task<Guid?> StartAsync(int? customSinceHours, CancellationToken cancellationToken = default) =>
        await SharedStartAsync(customSinceHours, cancellationToken);

    private async Task<Guid?> SharedStartAsync(int? customSinceHours, CancellationToken cancellationToken = default)
    {
        int sinceHours = customSinceHours ?? DataBridgeScanConfiguration.DailyScanIncludeChangesWithinTotalHours;
        var scanCorrelationId = Guid.NewGuid();

        Logger.LogInformation("Attempting to acquire lock for {LockName} with scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);

        var @lock = await _distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);


        if (@lock == null)
        {
            Logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);
            return null;
        }

        Logger.LogInformation("Lock acquired for {LockName}. Task started at {startTime} scanCorrelationId: {scanCorrelationId}.", LockName, DateTime.UtcNow, scanCorrelationId);

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

                        await ExecuteTaskAsync(@lock, scanCorrelationId, sinceHours, cts);
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
        int sinceHours = DataBridgeScanConfiguration.DailyScanIncludeChangesWithinTotalHours;
        var scanCorrelationId = Guid.NewGuid();

        Logger.LogInformation("Attempting to acquire lock for {LockName} scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);

        await using var @lock = await _distributedLock.TryAcquireAsync(LockName, s_lockDuration, cancellationToken);

        if (@lock == null)
        {
            Logger.LogInformation("Could not acquire lock for {LockName}, another instance is likely running scanCorrelationId: {scanCorrelationId}.", LockName, scanCorrelationId);
            return;
        }

        Logger.LogInformation("Lock acquired for {LockName}. Task started at {startTime} scanCorrelationId: {scanCorrelationId}.", LockName, DateTime.UtcNow, scanCorrelationId);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await ExecuteTaskAsync(@lock, scanCorrelationId, sinceHours, cts);
    }

    protected abstract Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        int sinceHours,
        CancellationTokenSource linkedCts);

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
                await _delayProvider.DelayAsync(s_renewalInterval, token);
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