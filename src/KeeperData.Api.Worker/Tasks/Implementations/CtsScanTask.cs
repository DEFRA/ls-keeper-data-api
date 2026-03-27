using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
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

public class CtsScanTask(
    CtsBulkScanOrchestrator bulkOrchestrator,
    CtsDailyScanOrchestrator dailyOrchestrator,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    IDelayProvider delayProvider,
    IScanStateRepository scanStateRepository,
    IApplicationMetrics metrics,
    ILogger<CtsScanTask> logger)
    : SmartScanTaskBase(
        dataBridgeScanConfiguration,
        distributedLock,
        applicationLifetime,
        delayProvider,
        scanStateRepository,
        metrics,
        logger), ICtsScanTask
{
    protected override string ScanSourceId => "cts-scan";
    protected override string LockName => nameof(CtsScanTask);

    protected override async Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        ScanMode scanMode,
        CancellationTokenSource linkedCts)
    {
        var externalToken = linkedCts.Token;
        var renewalTask = RenewLockPeriodicallyAsync(lockHandle, scanCorrelationId, linkedCts);

        try
        {
            int totalItems;

            if (scanMode.IsBulk)
            {
                totalItems = await ExecuteBulkScanAsync(scanCorrelationId, scanMode, linkedCts);
            }
            else
            {
                totalItems = await ExecuteDailyScanAsync(scanCorrelationId, scanMode, linkedCts);
            }

            await RecordScanStateAsync(scanCorrelationId, scanMode.ScanStartedAt, scanMode.ModeName, totalItems, linkedCts.Token);

            Logger.LogInformation("Import completed successfully at {EndTime}, scanCorrelationId: {ScanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
        }
        catch (OperationCanceledException) when (renewalTask.IsFaulted || (renewalTask.IsCompleted && !externalToken.IsCancellationRequested))
        {
            Logger.LogError("Import was stopped due to lock renewal failure at {EndTime}, scanCorrelationId: {ScanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
            throw new InvalidOperationException("Task was cancelled due to lock renewal failure");
        }
        catch (OperationCanceledException) when (externalToken.IsCancellationRequested)
        {
            Logger.LogInformation("Import was cancelled at {EndTime}, scanCorrelationId: {ScanCorrelationId}", DateTime.UtcNow, scanCorrelationId);
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
                Logger.LogError(ex, "Unexpected error in lock renewal task for {LockName} scanCorrelationId: {ScanCorrelationId}", LockName, scanCorrelationId);
            }
        }
    }

    private async Task<int> ExecuteBulkScanAsync(Guid scanCorrelationId, ScanMode scanMode, CancellationTokenSource linkedCts)
    {
        var context = new CtsBulkScanContext
        {
            ScanCorrelationId = scanCorrelationId,
            CurrentDateTime = scanMode.ScanStartedAt,
            UpdatedSinceDateTime = null,
            PageSize = DataBridgeScanConfiguration.QueryPageSize,
            Holdings = new()
        };

        await bulkOrchestrator.ExecuteAsync(context, linkedCts.Token);

        Metrics.RecordCount("scan_items_found", context.Holdings.CurrentSkip, ("scan_type", "CTS"), ("entity", "Holdings"), ("scan_mode", "bulk"));
        Metrics.RecordCount("scan_completed", 1, ("scan_type", "CTS"), ("scan_mode", "bulk"));

        return context.Holdings.CurrentSkip;
    }

    private async Task<int> ExecuteDailyScanAsync(Guid scanCorrelationId, ScanMode scanMode, CancellationTokenSource linkedCts)
    {
        var context = new CtsDailyScanContext
        {
            ScanCorrelationId = scanCorrelationId,
            CurrentDateTime = scanMode.ScanStartedAt,
            UpdatedSinceDateTime = scanMode.UpdatedSinceDateTime,
            PageSize = DataBridgeScanConfiguration.QueryPageSize,
            Holdings = new(),
            Agents = new(),
            Keepers = new()
        };

        await dailyOrchestrator.ExecuteAsync(context, linkedCts.Token);

        Metrics.RecordCount("scan_items_found", context.Holdings.CurrentSkip, ("scan_type", "CTS"), ("entity", "Holdings"), ("scan_mode", "daily"));
        Metrics.RecordCount("scan_items_found", context.Agents.CurrentSkip, ("scan_type", "CTS"), ("entity", "Agents"), ("scan_mode", "daily"));
        Metrics.RecordCount("scan_items_found", context.Keepers.CurrentSkip, ("scan_type", "CTS"), ("entity", "Keepers"), ("scan_mode", "daily"));
        Metrics.RecordCount("scan_completed", 1, ("scan_type", "CTS"), ("scan_mode", "daily"));

        return context.Holdings.CurrentSkip + context.Agents.CurrentSkip + context.Keepers.CurrentSkip;
    }
}