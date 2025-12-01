using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;

[StepOrder(3)]
public class SamPartyDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger<SamPartyDailyScanStep> logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    private readonly IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IDelayProvider _delayProvider = delayProvider;
    private readonly bool _samPartiesEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:SamPartiesEnabled");

    private const string SelectFields = "PARTY_ID";

    protected override async Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        if (!_samPartiesEnabled)
        {
            return;
        }

        context.Parties.CurrentTop = context.Parties.CurrentTop > 0
            ? context.Parties.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Parties.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetSamPartiesAsync<SamScanPartyIdentifier>(
                context.Parties.CurrentTop,
                context.Parties.CurrentSkip,
                SelectFields,
                context.UpdatedSinceDateTime,
                orderBy: null,
                cancellationToken);

            if (queryResponse == null || queryResponse.Data.Count == 0)
            {
                context.Parties.ScanCompleted = true;
                break;
            }

            var identifiers = queryResponse.Data
                .Select(x => x.PARTY_ID)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var id in identifiers)
            {
                var message = new SamUpdatePartyMessage
                {
                    Id = Guid.NewGuid(),
                    Identifier = id
                };

                await _intakeMessagePublisher.PublishAsync(message, cancellationToken);
            }

            context.Parties.TotalCount = queryResponse.TotalCount;
            context.Parties.CurrentCount = queryResponse.Count;
            context.Parties.CurrentSkip += queryResponse.Count;

            var hasReachedLimit = _dataBridgeScanConfiguration.LimitScanTotalBatchSize > 0
                && context.Parties.CurrentSkip >= _dataBridgeScanConfiguration.LimitScanTotalBatchSize;

            context.Parties.ScanCompleted = queryResponse.Count < context.Parties.CurrentTop || hasReachedLimit;

            if (!context.Parties.ScanCompleted
                && _dataBridgeScanConfiguration.DelayBetweenQueriesSeconds > 0)
            {
                await _delayProvider.DelayAsync(
                    TimeSpan.FromSeconds(_dataBridgeScanConfiguration.DelayBetweenQueriesSeconds),
                    cancellationToken);
            }
        }
    }
}