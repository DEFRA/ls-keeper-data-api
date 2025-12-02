using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
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

    private const string SelectFields = "LID_FULL_IDENTIFIER";
    private const string OrderBy = "LID_FULL_IDENTIFIER asc";

    protected override async Task ExecuteCoreAsync(CtsBulkScanContext context, CancellationToken cancellationToken)
    {
        context.Holdings.CurrentTop = context.Holdings.CurrentTop > 0
            ? context.Holdings.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Holdings.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetCtsHoldingsAsync<CtsScanHoldingIdentifier>(
                context.Holdings.CurrentTop,
                context.Holdings.CurrentSkip,
                SelectFields,
                context.UpdatedSinceDateTime,
                OrderBy,
                cancellationToken);

            if (queryResponse == null || queryResponse.Data.Count == 0)
            {
                context.Holdings.ScanCompleted = true;
                break;
            }

            var identifiers = queryResponse.Data
                .Select(x => x.LID_FULL_IDENTIFIER)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var id in identifiers)
            {
                var message = new CtsImportHoldingMessage
                {
                    Id = Guid.NewGuid(),
                    Identifier = id
                };

                await _intakeMessagePublisher.PublishAsync(message, cancellationToken);
            }

            context.Holdings.TotalCount = queryResponse.TotalCount;
            context.Holdings.CurrentCount = queryResponse.Count;
            context.Holdings.CurrentSkip += queryResponse.Count;

            var hasReachedLimit = _dataBridgeScanConfiguration.LimitScanTotalBatchSize > 0
                && context.Holdings.CurrentSkip >= _dataBridgeScanConfiguration.LimitScanTotalBatchSize;

            context.Holdings.ScanCompleted = queryResponse.Count < context.Holdings.CurrentTop || hasReachedLimit;

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