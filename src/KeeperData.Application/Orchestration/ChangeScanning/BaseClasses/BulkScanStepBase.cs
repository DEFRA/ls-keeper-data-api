using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.ChangeScanning.BaseClasses;

public abstract class BulkScanStepBase<TContext, THoldingIdentifier, TImportHoldingMessage>(
    IMessagePublisher<IntakeEventsQueueClient> intakeMessagePublisher,
    DataBridgeScanConfiguration dataBridgeScanConfiguration,
    IDelayProvider delayProvider,
    ILogger logger) : PaginatedScanStepBase<TContext, THoldingIdentifier>(
        intakeMessagePublisher,
        dataBridgeScanConfiguration,
        delayProvider,
        logger)
    where TContext : ScanContext, IBulkScanContext
    where TImportHoldingMessage : class
{

    protected abstract string SelectFields { get; }
    protected abstract string OrderBy { get; }

    protected override bool IsEntityEnabled() => true;

    protected override EntityScanContext GetScanContext(TContext context)
        => context.Holdings;

    protected override async Task<DataBridgeResponse<THoldingIdentifier>?> QueryDataAsync(
        TContext context,
        CancellationToken cancellationToken)
    {
        var scanState = GetScanContext(context);
        return await GetHoldingsAsync(
            scanState.CurrentTop,
            scanState.CurrentSkip,
            SelectFields,
            context.UpdatedSinceDateTime,
            OrderBy,
            cancellationToken);
    }

    protected override async Task PublishMessagesAsync(
        DataBridgeResponse<THoldingIdentifier> queryResponse,
        CancellationToken cancellationToken)
    {
        var identifiers = queryResponse.Data
            .Select(ExtractIdentifier)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        foreach (var message in identifiers.Select(CreateImportMessage))
            await IntakeMessagePublisher.PublishAsync(message, cancellationToken);
    }

    protected abstract Task<DataBridgeResponse<THoldingIdentifier>> GetHoldingsAsync(
        int top,
        int skip,
        string selectFields,
        DateTime? updatedSince,
        string orderBy,
        CancellationToken cancellationToken);

    protected abstract string ExtractIdentifier(THoldingIdentifier holdingIdentifier);

    protected abstract TImportHoldingMessage CreateImportMessage(string identifier);
}