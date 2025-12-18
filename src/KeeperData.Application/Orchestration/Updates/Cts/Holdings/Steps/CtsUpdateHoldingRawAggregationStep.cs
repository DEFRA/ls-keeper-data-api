using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Updates.Cts.Holdings.Steps;

[StepOrder(1)]
public class CtsUpdateHoldingRawAggregationStep(
    IDataBridgeClient dataBridgeClient,
    ILogger<CtsUpdateHoldingRawAggregationStep> logger) : UpdateStepBase<CtsUpdateHoldingContext>(logger)
{
    private readonly IDataBridgeClient _dataBridgeClient = dataBridgeClient;

    protected override async Task ExecuteCoreAsync(CtsUpdateHoldingContext context, CancellationToken cancellationToken)
    {
        var getHoldingsTask = _dataBridgeClient.GetCtsHoldingsAsync(context.Cph, cancellationToken);
        var getAgentsTask = _dataBridgeClient.GetCtsAgentsAsync(context.Cph, cancellationToken);
        var getKeepersTask = _dataBridgeClient.GetCtsKeepersAsync(context.Cph, cancellationToken);

        await Task.WhenAll(getHoldingsTask, getAgentsTask, getKeepersTask);

        context.RawHolding = getHoldingsTask.Result.FirstOrDefault();
        context.RawAgents = getAgentsTask.Result;

        var rawKeepers = getKeepersTask.Result;
        context.RawKeepers = DeduplicateKeepersByLatest(rawKeepers);

        var (originalCount, deduplicatedCount, duplicatesRemoved) =
            GetDebugStats(rawKeepers, context.RawKeepers);

        logger.LogWarning("Deduplicated {DuplicatesRemoved} keeper records for CPH {Cph}. Original: {OriginalCount}, Final: {DeduplicatedCount}",
            duplicatesRemoved, context.Cph, originalCount, deduplicatedCount);
    }

    private List<CtsAgentOrKeeper> DeduplicateKeepersByLatest(List<CtsAgentOrKeeper> keepers)
    {
        if (keepers == null || !keepers.Any())
            return keepers ?? new List<CtsAgentOrKeeper>();

        // Group by PAR_ID and select the latest record for each group
        var deduplicatedKeepers = keepers
            .GroupBy(k => k.PAR_ID)
            .Select(group => group
                .OrderByDescending(k => k.UpdatedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(k => k.CreatedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(k => k.BATCH_ID ?? 0)
                .First())
            .ToList();

        return deduplicatedKeepers;
    }

    private (int originalCount, int deduplicatedCount, int duplicatesRemoved) GetDebugStats(
        List<CtsAgentOrKeeper> original, List<CtsAgentOrKeeper> deduplicated)
    {
        var originalCount = original?.Count ?? 0;
        var deduplicatedCount = deduplicated?.Count ?? 0;
        var duplicatesRemoved = Math.Max(0, originalCount - deduplicatedCount);

        return (originalCount, deduplicatedCount, duplicatesRemoved);
    }
}