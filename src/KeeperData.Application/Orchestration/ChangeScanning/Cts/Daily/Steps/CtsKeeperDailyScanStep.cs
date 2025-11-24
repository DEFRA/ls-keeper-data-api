using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily.Steps;

[StepOrder(2)]
public class CtsKeeperDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    ILogger<CtsKeeperDailyScanStep> logger) : ScanStepBase<CtsDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    private readonly IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IDelayProvider _delayProvider = delayProvider;

    private const string SelectFields = "PAR_ID";

    protected override async Task ExecuteCoreAsync(CtsDailyScanContext context, CancellationToken cancellationToken)
    {
        context.Keepers.CurrentTop = context.Keepers.CurrentTop > 0
            ? context.Keepers.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Keepers.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetCtsKeepersAsync<CtsScanAgentOrKeeperIdentifier>(
                context.Keepers.CurrentTop,
                context.Keepers.CurrentSkip,
                SelectFields,
                context.UpdatedSinceDateTime,
                cancellationToken);

            if (queryResponse == null || queryResponse.Data.Count == 0)
            {
                context.Keepers.ScanCompleted = true;
                break;
            }

            var identifiers = queryResponse.Data
                .Select(x => x.PAR_ID)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var id in identifiers)
            {
                var message = new CtsUpdateKeeperMessage
                {
                    Id = Guid.NewGuid(),
                    Identifier = id
                };

                await _intakeMessagePublisher.PublishAsync(message, cancellationToken);
            }

            context.Keepers.TotalCount = queryResponse.TotalCount;
            context.Keepers.CurrentCount = queryResponse.Count;
            context.Keepers.CurrentSkip += queryResponse.Count;

            var hasReachedLimit = _dataBridgeScanConfiguration.LimitScanTotalBatchSize > 0
                && context.Keepers.CurrentSkip >= _dataBridgeScanConfiguration.LimitScanTotalBatchSize;

            context.Keepers.ScanCompleted = queryResponse.Count < context.Keepers.CurrentTop || hasReachedLimit;

            if (!context.Keepers.ScanCompleted
                && _dataBridgeScanConfiguration.DelayBetweenQueriesSeconds > 0)
            {
                await _delayProvider.DelayAsync(
                    TimeSpan.FromSeconds(_dataBridgeScanConfiguration.DelayBetweenQueriesSeconds),
                    cancellationToken);
            }
        }
    }
}