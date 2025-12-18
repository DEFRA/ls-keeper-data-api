using KeeperData.Application.Orchestration.Helpers;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Imports.Cts.Holdings.Steps;

[StepOrder(1)]
public class CtsHoldingImportRawAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsHoldingImportRawAggregationStep> logger) : ImportStepBase<CtsHoldingImportContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(CtsHoldingImportContext context, CancellationToken cancellationToken)
    {
        var getHoldingsTask = _dataBridgeClient.GetCtsHoldingsAsync(context.Cph, cancellationToken);
        var getAgentsTask = _dataBridgeClient.GetCtsAgentsAsync(context.Cph, cancellationToken);
        var getKeepersTask = _dataBridgeClient.GetCtsKeepersAsync(context.Cph, cancellationToken);

        await Task.WhenAll(
            getHoldingsTask,
            getAgentsTask,
            getKeepersTask);

        context.RawHoldings = getHoldingsTask.Result;
        context.RawAgents = getAgentsTask.Result;

        var rawKeepers = getKeepersTask.Result;
        context.RawKeepers = CtsKeeperDeduplicationHelper.DeduplicateKeepersByLatest(rawKeepers);

        var (originalCount, deduplicatedCount, duplicatesRemoved) =
            CtsKeeperDeduplicationHelper.GetDeduplicationStats(rawKeepers, context.RawKeepers);

        logger.LogWarning("Deduplicated {DuplicatesRemoved} keeper records for CPH {Cph}. Original: {OriginalCount}, Final: {DeduplicatedCount}",
            duplicatesRemoved, context.Cph, originalCount, deduplicatedCount);
    }
}