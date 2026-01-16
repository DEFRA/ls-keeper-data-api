using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily.Steps;

[StepOrder(3)]
public class CtsAgentDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger<CtsAgentDailyScanStep> logger) : ScanStepBase<CtsDailyScanContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;
    private readonly IMessagePublisher<IntakeEventsQueueClient> _intakeMessagePublisher = intakeMessagePublisher;
    private readonly DataBridgeScanConfiguration _dataBridgeScanConfiguration = dataBridgeScanConfiguration;
    private readonly IDelayProvider _delayProvider = delayProvider;
    private readonly bool _ctsAgentsEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:CtsAgentsEnabled");

    private const string SelectFields = "PAR_ID";
    private const string OrderBy = "LID_FULL_IDENTIFIER asc";

    protected override async Task ExecuteCoreAsync(CtsDailyScanContext context, CancellationToken cancellationToken)
    {
        if (!_ctsAgentsEnabled)
        {
            return;
        }

        context.Agents.CurrentTop = context.Agents.CurrentTop > 0
            ? context.Agents.CurrentTop
            : _dataBridgeScanConfiguration.QueryPageSize;

        while (!context.Agents.ScanCompleted && !cancellationToken.IsCancellationRequested)
        {
            var queryResponse = await _dataBridgeClient.GetCtsAgentsAsync<CtsScanAgentOrKeeperIdentifier>(
                context.Agents.CurrentTop,
                context.Agents.CurrentSkip,
                SelectFields,
                context.UpdatedSinceDateTime,
                OrderBy,
                cancellationToken);

            if (queryResponse == null || queryResponse.Data.Count == 0)
            {
                context.Agents.ScanCompleted = true;
                break;
            }

            var identifiers = queryResponse.Data
                .Select(x => x.PAR_ID)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            foreach (var id in identifiers)
            {
                var message = new CtsUpdateAgentMessage
                {
                    Id = Guid.NewGuid(),
                    Identifier = id
                };

                await _intakeMessagePublisher.PublishAsync(message, cancellationToken);
            }

            context.Agents.TotalCount = queryResponse.TotalCount;
            context.Agents.CurrentCount = queryResponse.Count;
            context.Agents.CurrentSkip += queryResponse.Count;

            var hasReachedLimit = _dataBridgeScanConfiguration.LimitScanTotalBatchSize > 0
                && context.Agents.CurrentSkip >= _dataBridgeScanConfiguration.LimitScanTotalBatchSize;

            context.Agents.ScanCompleted = queryResponse.Count < context.Agents.CurrentTop || hasReachedLimit;

            if (!context.Agents.ScanCompleted
                && _dataBridgeScanConfiguration.DelayBetweenQueriesSeconds > 0)
            {
                await _delayProvider.DelayAsync(
                    TimeSpan.FromSeconds(_dataBridgeScanConfiguration.DelayBetweenQueriesSeconds),
                    cancellationToken);
            }
        }
    }
}