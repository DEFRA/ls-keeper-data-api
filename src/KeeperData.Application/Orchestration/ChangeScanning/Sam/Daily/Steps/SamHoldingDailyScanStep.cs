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
    ILogger<SamHoldingDailyScanStep> logger) 
    : DailyScanStepBase<SamScanHoldingIdentifier>(
        dataBridgeClient, 
        intakeMessagePublisher, 
        dataBridgeScanConfiguration, 
        delayProvider, 
        configuration, 
        logger)
{
    private const string SelectFields = "CPH";
    private const string OrderBy = "CPH asc";

    protected override bool IsEntityEnabled()
        => Configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHoldingsEnabled");

    protected override EntityScanContext GetScanContext(SamDailyScanContext context)
        => context.Holdings;

    protected override async Task<DataBridgeResponse<SamScanHoldingIdentifier>?> QueryDataAsync(
        SamDailyScanContext context, 
        CancellationToken cancellationToken)
        => await DataBridgeClient.GetSamHoldingsAsync<SamScanHoldingIdentifier>(
            context.Holdings.CurrentTop,
            context.Holdings.CurrentSkip,
            SelectFields,
            context.UpdatedSinceDateTime,
            OrderBy,
            cancellationToken);

    protected override async Task PublishMessagesAsync(
        DataBridgeResponse<SamScanHoldingIdentifier> queryResponse, 
        CancellationToken cancellationToken)
    {
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

            await IntakeMessagePublisher.PublishAsync(message, cancellationToken);
        }
    }
}