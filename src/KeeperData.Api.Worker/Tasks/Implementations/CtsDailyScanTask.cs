using KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public class CtsDailyScanTask(
    CtsDailyScanOrchestrator orchestrator,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    IDelayProvider delayProvider,
    IScanStateRepository scanStateRepository,
    IApplicationMetrics metrics,
    ILogger<CtsDailyScanTask> logger)
    : DailyScanTaskBase(dataBridgeScanConfiguration, distributedLock, applicationLifetime, delayProvider, scanStateRepository, metrics, logger),
        ICtsDailyScanTask
{
    protected override string LockName => nameof(CtsDailyScanTask);
    protected override string ScanSourceId => "cts-scan";

    protected override async Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        int sinceHours,
        DateTime? updatedSinceDateTime,
        CancellationTokenSource linkedCts)
    {
        var externalToken = linkedCts.Token;
        var renewalTask = RenewLockPeriodicallyAsync(lockHandle, scanCorrelationId, linkedCts);
        var scanStartedAt = DateTime.UtcNow;

        try
        {
            var effectiveUpdatedSince = updatedSinceDateTime ?? DateTime.UtcNow.AddHours(-Math.Abs(sinceHours));

            Logger.LogInformation("Import for last {sinceHours} hours of records started at {endTime}, scanCorrelationId: {scanCorrelationId}", sinceHours, DateTime.UtcNow, scanCorrelationId);

            var context = new CtsDailyScanContext
            {
                ScanCorrelationId = scanCorrelationId,
                CurrentDateTime = DateTime.UtcNow,
                UpdatedSinceDateTime = effectiveUpdatedSince,
                PageSize = DataBridgeScanConfiguration.QueryPageSize,
                Holdings = new(),
                Agents = new(),
                Keepers = new()
            };

            await orchestrator.ExecuteAsync(context, linkedCts.Token);

            Metrics.RecordCount("daily_scan_items_found", context.Holdings.CurrentSkip, ("scan_type", "CTS"), ("entity", "Holdings"));
            Metrics.RecordCount("daily_scan_items_found", context.Agents.CurrentSkip, ("scan_type", "CTS"), ("entity", "Agents"));
            Metrics.RecordCount("daily_scan_items_found", context.Keepers.CurrentSkip, ("scan_type", "CTS"), ("entity", "Keepers"));
            Metrics.RecordCount("daily_scan_completed", 1, ("scan_type", "CTS"));

            var totalItems = context.Holdings.CurrentSkip + context.Agents.CurrentSkip + context.Keepers.CurrentSkip;
            await RecordScanStateAsync(scanCorrelationId, scanStartedAt, "daily", totalItems, linkedCts.Token);

            Logger.LogInformation("Import completed successfully at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
        }
        catch (OperationCanceledException) when (renewalTask.IsFaulted || (renewalTask.IsCompleted && !externalToken.IsCancellationRequested))
        {
            Logger.LogError("Import was stopped due to lock renewal failure at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
            throw new InvalidOperationException("Task was cancelled due to lock renewal failure");
        }
        catch (OperationCanceledException) when (externalToken.IsCancellationRequested)
        {
            Logger.LogInformation("Import was cancelled at {endTime}, scanCorrelationId: {scanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
            throw;
        }
        catch (RetryableException)
        {
            throw;
        }
        catch (NonRetryableException)
        {
            throw;
        }
        catch (Exception)
        {
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error in lock renewal task for {LockName} scanCorrelationId: {scanCorrelationId}", LockName, scanCorrelationId);
            }
        }
    }
}