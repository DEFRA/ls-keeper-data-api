using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public abstract class DailyScanTaskBase(
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDistributedLock distributedLock,
    IHostApplicationLifetime applicationLifetime,
    IDelayProvider delayProvider,
    IScanStateRepository scanStateRepository,
    IApplicationMetrics metrics,
    ILogger logger)
    : ScanTaskBase(dataBridgeScanConfiguration,
        distributedLock,
        applicationLifetime,
        delayProvider,
        scanStateRepository,
        metrics,
        logger), IDailyScanTask
{
    public Task<Guid?> StartAsync(CancellationToken cancellationToken = default) =>
        SharedStartAsync(null, cancellationToken);

    public Task<Guid?> StartAsync(int? customSinceHours, CancellationToken cancellationToken = default) =>
        SharedStartAsync(customSinceHours, cancellationToken);

    private async Task<Guid?> SharedStartAsync(int? customSinceHours, CancellationToken cancellationToken = default)
    {
        int sinceHours = customSinceHours ?? DataBridgeScanConfiguration.DailyScanIncludeChangesWithinTotalHours;
        DateTime? updatedSinceDateTime = null;

        if (customSinceHours == null)
        {
            updatedSinceDateTime = await GetUpdatedSinceDateTimeFromScanStateAsync(sinceHours, cancellationToken);
        }

        return await StartScanAsync(
            (lockHandle, scanCorrelationId, cts) =>
                ExecuteTaskAsync(lockHandle, scanCorrelationId, sinceHours, updatedSinceDateTime, cts),
            cancellationToken);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        int sinceHours = DataBridgeScanConfiguration.DailyScanIncludeChangesWithinTotalHours;
        var updatedSinceDateTime = await GetUpdatedSinceDateTimeFromScanStateAsync(sinceHours, cancellationToken);

        await RunScanAsync(
            (lockHandle, scanCorrelationId, cts) =>
                ExecuteTaskAsync(lockHandle, scanCorrelationId, sinceHours, updatedSinceDateTime, cts),
            cancellationToken);
    }

    protected abstract Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        int sinceHours,
        DateTime? updatedSinceDateTime,
        CancellationTokenSource linkedCts);

    private async Task<DateTime?> GetUpdatedSinceDateTimeFromScanStateAsync(int fallbackSinceHours, CancellationToken cancellationToken)
    {
        try
        {
            var scanState = await ScanStateRepository.GetByIdAsync(ScanSourceId, cancellationToken);
            if (scanState != null)
            {
                Logger.LogInformation(
                    "Using scan state for {ScanSourceId}: lastSuccessfulScanStartedAt={LastScanStartedAt}, " +
                    "lastScanMode={LastScanMode}, lastScanCorrelationId={LastCorrelationId}",
                    ScanSourceId, scanState.LastSuccessfulScanStartedAt, scanState.LastScanMode, scanState.LastScanCorrelationId);

                return scanState.LastSuccessfulScanStartedAt;
            }

            Logger.LogInformation(
                "No scan state found for {ScanSourceId}, falling back to configured window of {SinceHours} hours.",
                ScanSourceId, fallbackSinceHours);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex,
                "Failed to read scan state for {ScanSourceId}, falling back to configured window of {SinceHours} hours.",
                ScanSourceId, fallbackSinceHours);
        }

        return null;
    }
}