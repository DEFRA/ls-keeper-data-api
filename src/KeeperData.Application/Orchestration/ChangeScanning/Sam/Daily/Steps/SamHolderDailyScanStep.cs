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

[StepOrder(2)]
public class SamHolderDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger<SamHolderDailyScanStep> logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    private readonly IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IDelayProvider _delayProvider = delayProvider;
    private readonly bool _samHoldersEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHoldersEnabled");

    private const string SelectFields = "PARTY_ID";
    private const string OrderBy = "PARTY_ID asc";

    protected override async Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        if (!_samHoldersEnabled)
        {
            return;
        }

        context.Holders.CurrentTop = context.Holders.CurrentTop > 0
            ? context.Holders.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Holders.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetSamHoldersAsync<SamScanHolderIdentifier>(
                context.Holders.CurrentTop,
                context.Holders.CurrentSkip,
                SelectFields,
                context.UpdatedSinceDateTime,
                OrderBy,
                cancellationToken);

            if (queryResponse == null || queryResponse.Data.Count == 0)
            {
                context.Holders.ScanCompleted = true;
                break;
            }

            var groupedByParty = queryResponse.Data
                .Where(x => !string.IsNullOrWhiteSpace(x.CPHS))
                .GroupBy(x => x.PARTY_ID);

            foreach (var partyGroup in groupedByParty)
            {
                var partyId = partyGroup.Key;

                var cphs = partyGroup
                    .SelectMany(x => x.CphList)
                    .Distinct();

                foreach (var cph in cphs)
                {
                    var message = new SamUpdateHoldingMessage
                    {
                        Id = Guid.NewGuid(),
                        Identifier = cph,
                    };

                    await _intakeMessagePublisher.PublishAsync(message, cancellationToken);
                }
            }

            context.Holders.TotalCount = queryResponse.TotalCount;
            context.Holders.CurrentCount = queryResponse.Count;
            context.Holders.CurrentSkip += queryResponse.Count;

            var hasReachedLimit = _dataBridgeScanConfiguration.LimitScanTotalBatchSize > 0
                && context.Holders.CurrentSkip >= _dataBridgeScanConfiguration.LimitScanTotalBatchSize;

            context.Holders.ScanCompleted = queryResponse.Count < context.Holders.CurrentTop || hasReachedLimit;

            if (!context.Holders.ScanCompleted
                && _dataBridgeScanConfiguration.DelayBetweenQueriesSeconds > 0)
            {
                await _delayProvider.DelayAsync(
                    TimeSpan.FromSeconds(_dataBridgeScanConfiguration.DelayBetweenQueriesSeconds),
                    cancellationToken);
            }
        }
    }
}