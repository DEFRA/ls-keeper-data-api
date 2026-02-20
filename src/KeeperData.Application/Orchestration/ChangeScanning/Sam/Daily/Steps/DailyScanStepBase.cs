using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;

public abstract class DailyScanStepBase<TIdentifier>(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    protected readonly IDataBridgeClient DataBridgeClient = dataBridgeClient;
    protected readonly IMessagePublisher<IntakeEventsQueueClient> IntakeMessagePublisher = intakeMessagePublisher;
    protected readonly DataBridgeScanConfiguration DataBridgeScanConfiguration = dataBridgeScanConfiguration;
    protected readonly IDelayProvider DelayProvider = delayProvider;
    protected readonly IConfiguration Configuration = configuration;

    protected override async Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        if (!IsEntityEnabled())
            return;

        var scanState = GetScanContext(context);
        scanState.CurrentTop = scanState.CurrentTop > 0
            ? scanState.CurrentTop
            : DataBridgeScanConfiguration.QueryPageSize;

        while (!scanState.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await QueryDataAsync(context, cancellationToken);

            if (queryResponse == null || queryResponse.Data.Count == 0)
            {
                scanState.ScanCompleted = true;
                break;
            }

            await PublishMessagesAsync(queryResponse, cancellationToken);

            scanState.TotalCount = queryResponse.TotalCount;
            scanState.CurrentCount = queryResponse.Count;
            scanState.CurrentSkip += queryResponse.Count;

            var hasReachedLimit = DataBridgeScanConfiguration.LimitScanTotalBatchSize > 0
                && scanState.CurrentSkip >= DataBridgeScanConfiguration.LimitScanTotalBatchSize;

            scanState.ScanCompleted = queryResponse.Count < scanState.CurrentTop || hasReachedLimit;

            if (!scanState.ScanCompleted
                && DataBridgeScanConfiguration.DelayBetweenQueriesSeconds > 0)
            {
                await DelayProvider.DelayAsync(
                    TimeSpan.FromSeconds(DataBridgeScanConfiguration.DelayBetweenQueriesSeconds),
                    cancellationToken);
            }
        }
    }

    protected abstract bool IsEntityEnabled();
    protected abstract EntityScanContext GetScanContext(SamDailyScanContext context);
    protected abstract Task<DataBridgeResponse<TIdentifier>?> QueryDataAsync(SamDailyScanContext context, CancellationToken cancellationToken);
    protected abstract Task PublishMessagesAsync(DataBridgeResponse<TIdentifier> queryResponse, CancellationToken cancellationToken);
}