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
    private const string ScanType = "SAM";
    private const string EntityHoldings = "Holdings";
    private const string EntityHolders = "Holders";
    private const string EntityShowgrounds = "Showgrounds";
    private const string EntityCommonLands = "CommonLands";
    private const string EntityPorts = "Ports";
    private const string EntityHerds = "Herds";
    private const string EntityParties = "Parties";

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
            Holders = new(),
            Showgrounds = new(),
            CommonLands = new(),
            Ports = new()
        };

        await bulkOrchestrator.ExecuteAsync(context, linkedCts.Token);

        Metrics.RecordCount(MetricNames.ScanItemsFound, context.Holdings.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityHoldings), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Bulk));
        Metrics.RecordCount(MetricNames.ScanItemsFound, context.Holders.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityHolders), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Bulk));
        Metrics.RecordCount(MetricNames.ScanItemsFound, context.Showgrounds.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityShowgrounds), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Bulk));
        Metrics.RecordCount(MetricNames.ScanItemsFound, context.CommonLands.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityCommonLands), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Bulk));
        Metrics.RecordCount(MetricNames.ScanItemsFound, context.Ports.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityPorts), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Bulk));
        Metrics.RecordCount(MetricNames.ScanCompleted, 1, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Bulk));

        return context.Holdings.CurrentSkip + context.Holders.CurrentSkip
            + context.Showgrounds.CurrentSkip + context.CommonLands.CurrentSkip + context.Ports.CurrentSkip;
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

        Metrics.RecordCount(MetricNames.ScanItemsFound, context.Holdings.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityHoldings), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Daily));
        Metrics.RecordCount(MetricNames.ScanItemsFound, context.Holders.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityHolders), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Daily));
        Metrics.RecordCount(MetricNames.ScanItemsFound, context.Herds.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityHerds), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Daily));
        Metrics.RecordCount(MetricNames.ScanItemsFound, context.Parties.CurrentSkip, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.Entity, EntityParties), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Daily));
        Metrics.RecordCount(MetricNames.ScanCompleted, 1, (MetricNames.CommonTags.ScanType, ScanType), (MetricNames.CommonTags.ScanMode, MetricNames.ScanModes.Daily));

        return context.Holdings.CurrentSkip + context.Holders.CurrentSkip
            + context.Herds.CurrentSkip + context.Parties.CurrentSkip;
    }
}