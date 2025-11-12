using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk.Steps;

[StepOrder(1)]
public class CtsHoldingBulkScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    ILogger<CtsHoldingBulkScanStep> logger) : ScanStepBase<CtsBulkScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    private readonly IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IDelayProvider _delayProvider = delayProvider;

    private const string SelectFields = "BATCH_ID,LID_FULL_IDENTIFIER,UpdatedAtUtc";

    protected override async Task ExecuteCoreAsync(CtsBulkScanContext context, CancellationToken cancellationToken)
    {
        context.Holdings.CurrentTop = context.Holdings.CurrentTop > 0
            ? context.Holdings.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Holdings.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetCtsHoldingsAsync(
                context.Holdings.CurrentTop,
                context.Holdings.CurrentSkip,
                SelectFields,
                context.UpdatedSinceDateTime,
                cancellationToken);

            if (queryResponse == null || queryResponse.Data.Count == 0)
            {
                context.Holdings.ScanCompleted = true;
                break;
            }

            foreach (var item in queryResponse.Data)
            {
                if (string.IsNullOrWhiteSpace(item.LID_FULL_IDENTIFIER))
                    continue;

                var message = new CtsImportHoldingMessage
                {
                    Id = Guid.NewGuid(),
                    BatchId = item.BATCH_ID ?? 0,
                    Identifier = item.LID_FULL_IDENTIFIER
                };

                await _intakeMessagePublisher.PublishAsync(message, cancellationToken);
            }

            context.Holdings.TotalCount = queryResponse.TotalCount;
            context.Holdings.CurrentCount = queryResponse.Count;
            context.Holdings.CurrentSkip += queryResponse.Count;
            context.Holdings.ScanCompleted = queryResponse.Count < context.Holdings.CurrentTop;

            if (!context.Holdings.ScanCompleted 
                && _dataBridgeScanConfiguration.DelayBetweenQueriesSeconds > 0)
            {
                await _delayProvider.DelayAsync(
                    TimeSpan.FromSeconds(_dataBridgeScanConfiguration.DelayBetweenQueriesSeconds),
                    cancellationToken);
            }
        }
    }
}
