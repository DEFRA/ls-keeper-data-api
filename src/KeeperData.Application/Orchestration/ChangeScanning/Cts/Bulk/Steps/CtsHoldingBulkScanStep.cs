using KeeperData.Application.Orchestration.ChangeScanning.BaseClasses;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk.Steps;

[StepOrder(1)]
public class CtsHoldingBulkScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    ILogger<CtsHoldingBulkScanStep> logger)
    : BulkScanStepBase<CtsBulkScanContext, CtsScanHoldingIdentifier, CtsImportHoldingMessage>(
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        logger)
{

    protected override string SelectFields => "LID_FULL_IDENTIFIER";
    protected override string OrderBy => "LID_FULL_IDENTIFIER asc";

    protected override async Task<DataBridgeResponse<CtsScanHoldingIdentifier>> GetHoldingsAsync(
        int top,
        int skip,
        string selectFields,
        DateTime? updatedSince,
        string orderBy,
        CancellationToken cancellationToken)
    {
        var result = await dataBridgeClient.GetCtsHoldingsAsync<CtsScanHoldingIdentifier>(
            top,
            skip,
            selectFields,
            updatedSince,
            orderBy,
            cancellationToken);

        return result ?? new DataBridgeResponse<CtsScanHoldingIdentifier> { CollectionName = "CtsHoldings" };
    }

    protected override string ExtractIdentifier(CtsScanHoldingIdentifier holdingIdentifier)
    {
        return holdingIdentifier.LID_FULL_IDENTIFIER;
    }

    protected override CtsImportHoldingMessage CreateImportMessage(string identifier)
    {
        return new CtsImportHoldingMessage
        {
            Id = Guid.NewGuid(),
            Identifier = identifier
        };
    }
}