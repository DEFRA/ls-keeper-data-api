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

[StepOrder(4)]
public class SamHerdDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger<SamHerdDailyScanStep> logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    private readonly IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IDelayProvider _delayProvider = delayProvider;
    private readonly bool _samHerdsEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHerdsEnabled");

    private const string SelectFields = "CPHH";

    protected override async Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        if (!_samHerdsEnabled)
        {
            return;
        }

        context.Herds.CurrentTop = context.Herds.CurrentTop > 0
            ? context.Herds.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Herds.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetSamHerdsAsync<SamScanHerdIdentifier>(
                context.Herds.CurrentTop,
                context.Herds.CurrentSkip,
                SelectFields,
                context.UpdatedSinceDateTime,
                cancellationToken);

            if (queryResponse == null || queryResponse.Data.Count == 0)
            {
                context.Herds.ScanCompleted = true;
                break;
            }

            var identifiers = queryResponse.Data
                .Select(x => x.CPHH)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var id in identifiers)
            {
                var message = new SamUpdateHerdMessage
                {
                    Id = Guid.NewGuid(),
                    Identifier = id
                };

                await _intakeMessagePublisher.PublishAsync(message, cancellationToken);
            }

            context.Herds.TotalCount = queryResponse.TotalCount;
            context.Herds.CurrentCount = queryResponse.Count;
            context.Herds.CurrentSkip += queryResponse.Count;

            var hasReachedLimit = _dataBridgeScanConfiguration.LimitScanTotalBatchSize > 0
                && context.Herds.CurrentSkip >= _dataBridgeScanConfiguration.LimitScanTotalBatchSize;

            context.Herds.ScanCompleted = queryResponse.Count < context.Herds.CurrentTop || hasReachedLimit;

            if (!context.Herds.ScanCompleted
                && _dataBridgeScanConfiguration.DelayBetweenQueriesSeconds > 0)
            {
                await _delayProvider.DelayAsync(
                    TimeSpan.FromSeconds(_dataBridgeScanConfiguration.DelayBetweenQueriesSeconds),
                    cancellationToken);
            }
        }
    }
}