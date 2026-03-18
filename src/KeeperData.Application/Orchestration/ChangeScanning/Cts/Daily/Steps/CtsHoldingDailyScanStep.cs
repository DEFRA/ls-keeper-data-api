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

[StepOrder(1)]
public class CtsHoldingDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger<CtsHoldingDailyScanStep> logger)
    : DailyScanStepBase<CtsDailyScanContext, CtsScanHoldingIdentifier>(
        dataBridgeClient,
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        configuration,
        logger)
{
    private const string SelectFields = "LID_FULL_IDENTIFIER";
    private const string OrderBy = "LID_FULL_IDENTIFIER asc";

    protected override bool IsEntityEnabled() => true;

    protected override EntityScanContext GetScanContext(CtsDailyScanContext context)
        => context.Holdings;

    protected override Task<DataBridgeResponse<CtsScanHoldingIdentifier>?> QueryDataAsync(
        CtsDailyScanContext context,
        CancellationToken cancellationToken)
    {
        var scanState = GetScanContext(context);
        return DataBridgeClient.GetCtsHoldingsAsync<CtsScanHoldingIdentifier>(
            scanState.CurrentTop,
            scanState.CurrentSkip,
            SelectFields,
            context.UpdatedSinceDateTime,
            OrderBy,
            cancellationToken);
    }

    protected override async Task PublishMessagesAsync(
        DataBridgeResponse<CtsScanHoldingIdentifier> queryResponse,
        CancellationToken cancellationToken)
    {
        var identifiers = queryResponse.Data
            .Select(x => x.LID_FULL_IDENTIFIER)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        foreach (var id in identifiers)
        {
            var message = new CtsUpdateHoldingMessage
            {
                Id = Guid.NewGuid(),
                Identifier = id
            };

            await IntakeMessagePublisher.PublishAsync(message, cancellationToken);
        }
    }
}