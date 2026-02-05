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
    ILogger<SamHolderDailyScanStep> logger)
    : DailyScanStepBase<SamScanHolderIdentifier>(
        dataBridgeClient,
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        configuration,
        logger)
{
    private const string SelectFields = "PARTY_ID,CPHS";
    private const string OrderBy = "PARTY_ID asc";

    protected override bool IsEntityEnabled()
        => Configuration.GetValue<bool>("DataBridgeCollectionFlags:SamHoldersEnabled");

    protected override EntityScanContext GetScanContext(SamDailyScanContext context)
        => context.Holders;

    protected override async Task<DataBridgeResponse<SamScanHolderIdentifier>?> QueryDataAsync(
        SamDailyScanContext context,
        CancellationToken cancellationToken)
        => await DataBridgeClient.GetSamHoldersAsync<SamScanHolderIdentifier>(
            context.Holders.CurrentTop,
            context.Holders.CurrentSkip,
            SelectFields,
            context.UpdatedSinceDateTime,
            OrderBy,
            cancellationToken);

    protected override async Task PublishMessagesAsync(
        DataBridgeResponse<SamScanHolderIdentifier> queryResponse,
        CancellationToken cancellationToken)
    {
        var groupedByParty = queryResponse.Data
            .Where(x => !string.IsNullOrWhiteSpace(x.CPHS))
            .GroupBy(x => x.PARTY_ID);

        foreach (var partyGroup in groupedByParty)
        {
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

                await IntakeMessagePublisher.PublishAsync(message, cancellationToken);
            }
        }
    }
}