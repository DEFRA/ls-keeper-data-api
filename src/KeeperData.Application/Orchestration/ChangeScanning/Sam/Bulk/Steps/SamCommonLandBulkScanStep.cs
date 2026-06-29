using KeeperData.Application.Orchestration.ChangeScanning.BaseClasses;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk.Steps;

[StepOrder(3)]
public class SamCommonLandBulkScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    ILogger<SamCommonLandBulkScanStep> logger)
    : BulkScanStepBase<SamBulkScanContext, SamScanCommonLandIdentifier, SamImportHoldingMessage>(
        dataBridgeClient,
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        logger)
{
    protected override string SelectFields => "COMMON_CPH";
    protected override string OrderBy => "COMMON_CPH asc";

    protected override EntityScanContext GetScanContext(SamBulkScanContext context) => context.CommonLands;

    protected override async Task<DataBridgeResponse<SamScanCommonLandIdentifier>> GetHoldingsAsync(
        int top,
        int skip,
        string selectFields,
        DateTime? updatedSince,
        string orderBy,
        CancellationToken cancellationToken)
    {
        var result = await DataBridgeClient.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(
            top,
            skip,
            selectFields,
            updatedSince,
            orderBy,
            cancellationToken);

        return result ?? new DataBridgeResponse<SamScanCommonLandIdentifier> { CollectionName = "SamCommonLands" };
    }

    protected override string ExtractIdentifier(SamScanCommonLandIdentifier holdingIdentifier)
    {
        return holdingIdentifier.COMMON_CPH;
    }

    protected override SamImportHoldingMessage CreateImportMessage(string identifier)
    {
        return new SamImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = identifier
        };
    }
}
