using KeeperData.Application.Orchestration.ChangeScanning.BaseClasses;
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
    ILogger<CtsAgentDailyScanStep> logger)
    : DailyScanStepBase<CtsDailyScanContext, CtsScanAgentOrKeeperIdentifier>(
        dataBridgeClient,
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        configuration,
        logger)
{
    private readonly bool _ctsAgentsEnabled = configuration.GetValue<bool>("DataBridgeCollectionFlags:CtsAgentsEnabled");

    private const string SelectFields = "PAR_ID";
    private const string OrderBy = "LID_FULL_IDENTIFIER asc";

    protected override bool IsEntityEnabled() => _ctsAgentsEnabled;

    protected override EntityScanContext GetScanContext(CtsDailyScanContext context)
        => context.Agents;

    protected override Task<DataBridgeResponse<CtsScanAgentOrKeeperIdentifier>?> QueryDataAsync(
        CtsDailyScanContext context,
        CancellationToken cancellationToken)
    {
        var scanState = GetScanContext(context);
        return DataBridgeClient.GetCtsAgentsAsync<CtsScanAgentOrKeeperIdentifier>(
            scanState.CurrentTop,
            scanState.CurrentSkip,
            SelectFields,
            context.UpdatedSinceDateTime,
            OrderBy,
            cancellationToken);
    }

    protected override async Task PublishMessagesAsync(
        DataBridgeResponse<CtsScanAgentOrKeeperIdentifier> queryResponse,
        CancellationToken cancellationToken)
    {
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

            await IntakeMessagePublisher.PublishAsync(message, cancellationToken);
        }
    }
}