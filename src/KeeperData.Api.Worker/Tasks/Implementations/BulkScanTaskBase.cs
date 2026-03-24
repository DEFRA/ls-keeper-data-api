using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Locking;
using KeeperData.Core.Providers;
using KeeperData.Core.Repositories;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeeperData.Api.Worker.Tasks.Implementations;

public abstract class BulkScanTaskBase(
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
        logger), IScanTask
{
    public Task<Guid?> StartAsync(CancellationToken cancellationToken = default) =>
        StartScanAsync(ExecuteTaskAsync, cancellationToken);

    public Task RunAsync(CancellationToken cancellationToken) =>
        RunScanAsync(ExecuteTaskAsync, cancellationToken);

    protected abstract Task ExecuteTaskAsync(
        IDistributedLockHandle lockHandle,
        Guid scanCorrelationId,
        CancellationTokenSource linkedCts);
}