using KeeperData.Application.Orchestration.ChangeScanning.BaseClasses;
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

[StepOrder(5)]
public class SamCommonLandDailyScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    IConfiguration configuration,
    ILogger<SamCommonLandDailyScanStep> logger)
    : DailyScanStepBase<SamDailyScanContext, SamScanCommonLandIdentifier>(dataBridgeClient, intakeMessagePublisher, dataBridgeScanConfiguration,
        delayProvider, configuration, logger)
{
    private const string SelectFields = "COMMON_CPH";
    private const string OrderBy = "COMMON_CPH asc";

    protected override bool IsEntityEnabled()
        => Configuration.GetValue<bool>("DataBridgeCollectionFlags:SamCommonLandsEnabled");

    protected override EntityScanContext GetScanContext(SamDailyScanContext context)
        => context.CommonLands;

    protected override async Task<DataBridgeResponse<SamScanCommonLandIdentifier>?> QueryDataAsync(
        SamDailyScanContext context,
        CancellationToken cancellationToken)
        => await DataBridgeClient.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(
            context.CommonLands.CurrentTop,
            context.CommonLands.CurrentSkip,
            SelectFields,
            context.UpdatedSinceDateTime,
            OrderBy,
            cancellationToken);

    protected override async Task PublishMessagesAsync(
        DataBridgeResponse<SamScanCommonLandIdentifier> queryResponse,
        CancellationToken cancellationToken)
    {
        var identifiers = queryResponse.Data
            .Select(x => x.COMMON_CPH)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        foreach (var id in identifiers)
        {
            var message = new SamUpdateHoldingMessage { Id = Guid.NewGuid(), Identifier = id };

            await IntakeMessagePublisher.PublishAsync(message, cancellationToken);
        }
    }
}
