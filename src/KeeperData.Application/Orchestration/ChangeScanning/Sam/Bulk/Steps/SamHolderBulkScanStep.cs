using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk.Steps;

[StepOrder(1)]
public class SamHolderBulkScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    ILogger<SamHolderBulkScanStep> logger) : ScanStepBase<SamBulkScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    private readonly IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IDelayProvider _delayProvider = delayProvider;

    private const string SelectFields = "PARTY_ID";

    protected override async Task ExecuteCoreAsync(SamBulkScanContext context, CancellationToken cancellationToken)
    {
        context.Holdings.CurrentTop = context.Holdings.CurrentTop > 0
            ? context.Holdings.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Holdings.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetSamHoldersAsync(
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

            var identifiers = queryResponse.Data
                .Select(x => x.PARTY_ID)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var id in identifiers)
            {
                var message = new SamImportHolderMessage
                {
                    Id = Guid.NewGuid(),
                    Identifier = id
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
