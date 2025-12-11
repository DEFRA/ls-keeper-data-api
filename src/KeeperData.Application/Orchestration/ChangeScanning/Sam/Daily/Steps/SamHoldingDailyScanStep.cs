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

[StepOrder(1)]
public class SamHoldingDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger<SamHoldingDailyScanStep> logger) : ScanStepBase<SamDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    private readonly IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IDelayProvider _delayProvider = delayProvider;
    private readonly bool _samHoldingsEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHoldingsEnabled");

    private const string SelectFields = "CPH";
    private const string OrderBy = "CPH asc";

    protected override async Task ExecuteCoreAsync(SamDailyScanContext context, CancellationToken cancellationToken)
    {
        if (!_samHoldingsEnabled)
        {
            return;
        }

        context.Holdings.CurrentTop = context.Holdings.CurrentTop > 0
            ? context.Holdings.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Holdings.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetSamHoldingsAsync<SamScanHoldingIdentifier>(
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
                .Select(x => x.CPH)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var id in identifiers)
            {
                var message = new SamUpdateHoldingMessage
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