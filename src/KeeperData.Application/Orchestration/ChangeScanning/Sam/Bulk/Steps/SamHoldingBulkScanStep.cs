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

[StepOrder(1)]
public class SamHoldingBulkScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    ILogger<SamHoldingBulkScanStep> logger)
    : BulkScanStepBase<SamBulkScanContext, SamScanHoldingIdentifier, SamImportHoldingMessage>(intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        logger)
{

    protected override string SelectFields => "CPH";
    protected override string OrderBy => "CPH asc";

    protected override async Task<DataBridgeResponse<SamScanHoldingIdentifier>> GetHoldingsAsync(
        int top,
        int skip,
        string selectFields,
        DateTime? updatedSince,
        string orderBy,
        CancellationToken cancellationToken)
    {
        var result = await dataBridgeClient.GetSamHoldingsAsync<SamScanHoldingIdentifier>(
            top,
            skip,
            selectFields,
            updatedSince,
            orderBy,
            cancellationToken);

        return result ?? new DataBridgeResponse<SamScanHoldingIdentifier>{ CollectionName = "SamHoldings" };
    }

    protected override string ExtractIdentifier(SamScanHoldingIdentifier holdingIdentifier)
    {
        return holdingIdentifier.CPH;
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