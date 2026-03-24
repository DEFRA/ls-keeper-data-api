using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public class SamDailyScanTask(
    SamDailyScanOrchestrator orchestrator,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    IDelayProvider delayProvider,
    IScanStateRepository scanStateRepository,
    IApplicationMetrics metrics,
    ILogger<SamDailyScanTask> logger)
    : DailyScanTaskBase(dataBridgeScanConfiguration, distributedLock, applicationLifetime, delayProvider, scanStateRepository, metrics, logger),
        ISamDailyScanTask
{
    protected override string LockName => nameof(SamDailyScanTask);
    protected override string ScanSourceId => "sam-scan";

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

            var context = new SamDailyScanContext
            {
                ScanCorrelationId = scanCorrelationId,
                CurrentDateTime = DateTime.UtcNow,
                UpdatedSinceDateTime = effectiveUpdatedSince,
                PageSize = DataBridgeScanConfiguration.QueryPageSize,
                Holdings = new(),
                Holders = new(),
                Herds = new(),
                Parties = new()
            };

            await orchestrator.ExecuteAsync(context, linkedCts.Token);

            // Daily scan progress metrics for grafana
            Metrics.RecordCount("daily_scan_items_found", context.Holdings.CurrentSkip, ("scan_type", "SAM"), ("entity", "Holdings"));
            Metrics.RecordCount("daily_scan_items_found", context.Holders.CurrentSkip, ("scan_type", "SAM"), ("entity", "Holders"));
            Metrics.RecordCount("daily_scan_items_found", context.Herds.CurrentSkip, ("scan_type", "SAM"), ("entity", "Herds"));
            Metrics.RecordCount("daily_scan_items_found", context.Parties.CurrentSkip, ("scan_type", "SAM"), ("entity", "Parties"));
            Metrics.RecordCount("daily_scan_completed", 1, ("scan_type", "SAM"));

            var totalItems = context.Holdings.CurrentSkip + context.Holders.CurrentSkip
                + context.Herds.CurrentSkip + context.Parties.CurrentSkip;
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