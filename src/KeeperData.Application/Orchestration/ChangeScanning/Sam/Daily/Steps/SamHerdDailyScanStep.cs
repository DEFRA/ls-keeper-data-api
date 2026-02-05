using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using KeeperData.Core.Domain.Sites.Formatters;
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
    ILogger<SamHerdDailyScanStep> logger)
    : DailyScanStepBase<SamScanHerdIdentifier>(
        dataBridgeClient,
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        configuration,
        logger)
{
    private const string SelectFields = "CPHH";
    private const string OrderBy = "CPHH asc";

    protected override bool IsEntityEnabled()
        => Configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHerdsEnabled");

    protected override EntityScanContext GetScanContext(SamDailyScanContext context)
        => context.Herds;

    protected override async Task<DataBridgeResponse<SamScanHerdIdentifier>?> QueryDataAsync(
        SamDailyScanContext context,
        CancellationToken cancellationToken)
        => await DataBridgeClient.GetSamHerdsAsync<SamScanHerdIdentifier>(
            context.Herds.CurrentTop,
            context.Herds.CurrentSkip,
            SelectFields,
            context.UpdatedSinceDateTime,
            OrderBy,
            cancellationToken);

    protected override async Task PublishMessagesAsync(
        DataBridgeResponse<SamScanHerdIdentifier> queryResponse,
        CancellationToken cancellationToken)
    {
        var identifiers = queryResponse.Data
            .Select(x => x.CPHH.CphhToCph())
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