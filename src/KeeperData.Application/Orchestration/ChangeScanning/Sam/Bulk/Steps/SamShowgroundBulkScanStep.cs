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

[StepOrder(2)]
public class SamShowgroundBulkScanStep(
    IDataBridgeClient dataBridgeClient,
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    ILogger<SamShowgroundBulkScanStep> logger)
    : BulkScanStepBase<SamBulkScanContext, SamScanShowgroundIdentifier, SamImportHoldingMessage>(
        dataBridgeClient,
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        logger)
{
    protected override string SelectFields => "CPH";
    protected override string OrderBy => "CPH asc";

    protected override EntityScanContext GetScanContext(SamBulkScanContext context) => context.Showgrounds;
    protected override async Task<DataBridgeResponse<SamScanShowgroundIdentifier>> GetHoldingsAsync(
        int top,
        int skip,
        string selectFields,
        DateTime? updatedSince,
        string orderBy,
        CancellationToken cancellationToken)
    {
        var result = await DataBridgeClient.GetSamShowgroundsAsync<SamScanShowgroundIdentifier>(
            top,
            skip,
            selectFields,
            updatedSince,
            orderBy,
            cancellationToken);

        return result ?? new DataBridgeResponse<SamScanShowgroundIdentifier> { CollectionName = "SamShowgrounds" };
    }

    protected override string ExtractIdentifier(SamScanShowgroundIdentifier holdingIdentifier)
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