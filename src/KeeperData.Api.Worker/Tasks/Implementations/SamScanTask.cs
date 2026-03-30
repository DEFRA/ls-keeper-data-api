using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public class SamScanTask(
    SamBulkScanOrchestrator bulkOrchestrator,
    SamDailyScanOrchestrator dailyOrchestrator,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    IDelayProvider delayProvider,
    IScanStateRepository scanStateRepository,
    IApplicationMetrics metrics,
    ILogger<SamScanTask> logger)
    : SmartScanTaskBase(
        dataBridgeScanConfiguration,
        distributedLock,
        applicationLifetime,
        delayProvider,
        scanStateRepository,
        metrics,
        logger), ISamScanTask
{
    protected override string ScanSourceId => "sam-scan";
    protected override string LockName => nameof(SamScanTask);

    protected override async Task<int> ExecuteBulkScanAsync(Guid scanCorrelationId, ScanMode scanMode, CancellationTokenSource linkedCts)
    {
        var context = new SamBulkScanContext
        {
            ScanCorrelationId = scanCorrelationId,
            CurrentDateTime = scanMode.ScanStartedAt,
            UpdatedSinceDateTime = null,
            PageSize = DataBridgeScanConfiguration.QueryPageSize,
            Holdings = new(),
            Holders = new()
        };

        await bulkOrchestrator.ExecuteAsync(context, linkedCts.Token);

        Metrics.RecordCount("scan_items_found", context.Holdings.CurrentSkip, ("scan_type", "SAM"), ("entity", "Holdings"), ("scan_mode", "bulk"));
        Metrics.RecordCount("scan_items_found", context.Holders.CurrentSkip, ("scan_type", "SAM"), ("entity", "Holders"), ("scan_mode", "bulk"));
        Metrics.RecordCount("scan_completed", 1, ("scan_type", "SAM"), ("scan_mode", "bulk"));

        return context.Holdings.CurrentSkip + context.Holders.CurrentSkip;
    }

    protected override async Task<int> ExecuteDailyScanAsync(Guid scanCorrelationId, ScanMode scanMode, CancellationTokenSource linkedCts)
    {
        var context = new SamDailyScanContext
        {
            ScanCorrelationId = scanCorrelationId,
            CurrentDateTime = scanMode.ScanStartedAt,
            UpdatedSinceDateTime = scanMode.UpdatedSinceDateTime,
            PageSize = DataBridgeScanConfiguration.QueryPageSize,
            Holdings = new(),
            Holders = new(),
            Herds = new(),
            Parties = new()
        };

        await dailyOrchestrator.ExecuteAsync(context, linkedCts.Token);

        Metrics.RecordCount("scan_items_found", context.Holdings.CurrentSkip, ("scan_type", "SAM"), ("entity", "Holdings"), ("scan_mode", "daily"));
        Metrics.RecordCount("scan_items_found", context.Holders.CurrentSkip, ("scan_type", "SAM"), ("entity", "Holders"), ("scan_mode", "daily"));
        Metrics.RecordCount("scan_items_found", context.Herds.CurrentSkip, ("scan_type", "SAM"), ("entity", "Herds"), ("scan_mode", "daily"));
        Metrics.RecordCount("scan_items_found", context.Parties.CurrentSkip, ("scan_type", "SAM"), ("entity", "Parties"), ("scan_mode", "daily"));
        Metrics.RecordCount("scan_completed", 1, ("scan_type", "SAM"), ("scan_mode", "daily"));

        return context.Holdings.CurrentSkip + context.Holders.CurrentSkip
            + context.Herds.CurrentSkip + context.Parties.CurrentSkip;
    }
}