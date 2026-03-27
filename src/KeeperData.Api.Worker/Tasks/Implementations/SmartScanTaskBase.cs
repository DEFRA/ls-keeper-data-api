using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public abstract class SmartScanTaskBase(
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    IDelayProvider delayProvider,
    IScanStateRepository scanStateRepository,
    IApplicationMetrics metrics,
    ILogger logger)
    : ScanTaskBase(
        dataBridgeScanConfiguration,
        distributedLock,
        applicationLifetime,
        delayProvider,
        scanStateRepository,
        metrics,
        logger), ISmartScanTask
{
    public async Task<Guid?> StartAsync(
        bool forceBulk = false,
        int? sinceHours = null,
        CancellationToken cancellationToken = default)
    {
        var scanMode = await DetermineScanModeAsync(forceBulk, sinceHours, cancellationToken);

        return await StartScanAsync(
            (lockHandle, scanCorrelationId, cts) =>
                ExecuteTaskAsync(lockHandle, scanCorrelationId, scanMode, cts),
            cancellationToken);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var scanMode = await DetermineScanModeAsync(forceBulk: false, sinceHours: null, cancellationToken);

        await RunScanAsync(
            (lockHandle, scanCorrelationId, cts) =>
                ExecuteTaskAsync(lockHandle, scanCorrelationId, scanMode, cts),
            cancellationToken);
    }

    protected abstract Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        ScanMode scanMode,
        CancellationTokenSource linkedCts);

    private async Task<ScanMode> DetermineScanModeAsync(
        bool forceBulk,
        int? sinceHours,
        CancellationToken cancellationToken)
    {
        if (forceBulk)
        {
            Logger.LogInformation(
                "Force bulk mode requested for {ScanSourceId}",
                ScanSourceId);
            return ScanMode.Bulk(DateTime.UtcNow);
        }

        if (sinceHours != null)
        {
            var updatedSince = DateTime.UtcNow.AddHours(-Math.Abs(sinceHours.Value));
            Logger.LogInformation(
                "Manual sinceHours={SinceHours} override for {ScanSourceId}, scanning since {UpdatedSince}",
                sinceHours.Value, ScanSourceId, updatedSince);
            return ScanMode.Daily(DateTime.UtcNow, updatedSince);
        }

        // Smart mode: check scan state
        try
        {
            var scanState = await ScanStateRepository.GetByIdAsync(ScanSourceId, cancellationToken);
            if (scanState != null)
            {
                Logger.LogInformation(
                    "Using scan state for {ScanSourceId}: lastSuccessfulScanStartedAt={LastScanStartedAt}, " +
                    "lastScanMode={LastScanMode}, lastScanCorrelationId={LastCorrelationId}",
                    ScanSourceId, scanState.LastSuccessfulScanStartedAt, scanState.LastScanMode, scanState.LastScanCorrelationId);

                return ScanMode.Daily(DateTime.UtcNow, scanState.LastSuccessfulScanStartedAt);
            }

            Logger.LogInformation(
                "No scan state found for {ScanSourceId}, defaulting to bulk mode (first run)",
                ScanSourceId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex,
                "Failed to read scan state for {ScanSourceId}, defaulting to bulk mode",
                ScanSourceId);
        }

        return ScanMode.Bulk(DateTime.UtcNow);
    }
}

public sealed class ScanMode
{
    public bool IsBulk { get; private init; }
    public string ModeName => IsBulk ? "bulk" : "daily";
    public DateTime ScanStartedAt { get; private init; }

    /// <summary>
    /// The date/time to scan changes since. Null for bulk mode (fetch everything).
    /// </summary>
    public DateTime? UpdatedSinceDateTime { get; private init; }

    public static ScanMode Bulk(DateTime scanStartedAt) => new()
    {
        IsBulk = true,
        ScanStartedAt = scanStartedAt,
        UpdatedSinceDateTime = null
    };

    public static ScanMode Daily(DateTime scanStartedAt, DateTime updatedSinceDateTime) => new()
    {
        IsBulk = false,
        ScanStartedAt = scanStartedAt,
        UpdatedSinceDateTime = updatedSinceDateTime
    };
}