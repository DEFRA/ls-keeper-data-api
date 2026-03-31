using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
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

    protected override async Task<int> ExecuteBulkScanAsync(Guid scanCorrelationId, ScanMode scanMode, CancellationTokenSource linkedCts)
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

    protected override async Task<int> ExecuteDailyScanAsync(Guid scanCorrelationId, ScanMode scanMode, CancellationTokenSource linkedCts)
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