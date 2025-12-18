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
        var singleKeeper = DeduplicateToSingleKeeper(rawKeepers);
        context.RawKeepers = singleKeeper != null ? [singleKeeper] : [];

        var (originalCount, finalCount, duplicatesRemoved) =
            GetDebugStats(rawKeepers, context.RawKeepers);

        logger.LogWarning("Deduplicated {DuplicatesRemoved} keeper records for CPH {Cph}. Original: {OriginalCount}, Final: {FinalCount}",
            duplicatesRemoved, context.Cph, originalCount, finalCount);
    }

    private CtsAgentOrKeeper? DeduplicateToSingleKeeper(List<CtsAgentOrKeeper> keepers)
    {
        if (keepers == null || !keepers.Any())
            return null;

        // Group by PAR_ID, get latest per keeper, then pick most recent keeper overall
        var singleKeeper = keepers
            .GroupBy(k => k.PAR_ID)
            .Select(group => group
                .OrderByDescending(k => k.UpdatedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(k => k.CreatedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(k => k.BATCH_ID ?? 0)
                .First())
            .OrderByDescending(k => k.UpdatedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(k => k.CreatedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(k => k.BATCH_ID ?? 0)
            .FirstOrDefault();

        return singleKeeper;
    }

    private (int originalCount, int finalCount, int duplicatesRemoved) GetDebugStats(
        List<CtsAgentOrKeeper> original, List<CtsAgentOrKeeper> final)
    {
        var originalCount = original?.Count ?? 0;
        var finalCount = final?.Count ?? 0;
        var duplicatesRemoved = Math.Max(0, originalCount - finalCount);

        return (originalCount, finalCount, duplicatesRemoved);
    }
}