using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;

namespace KeeperData.Application.Orchestration.Helpers;

public static class CtsKeeperDeduplicationHelper
{
    public static List<CtsAgentOrKeeper> DeduplicateKeepersByLatest(List<CtsAgentOrKeeper>? keepers)
    {
        if (keepers?.Any() != true)
            return new List<CtsAgentOrKeeper>();

        return keepers
            .Where(k => !string.IsNullOrWhiteSpace(k.PAR_ID))
            .GroupBy(k => k.PAR_ID)
            .Select(group => group
                .OrderByDescending(k => k.UpdatedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(k => k.CreatedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(k => k.BATCH_ID ?? 0)
                .First())
            .ToList();
    }

    public static (int originalCount, int deduplicatedCount, int duplicatesRemoved) GetDeduplicationStats(
        List<CtsAgentOrKeeper>? originalKeepers,
        List<CtsAgentOrKeeper>? deduplicatedKeepers)
    {
        var originalCount = originalKeepers?.Count ?? 0;
        var deduplicatedCount = deduplicatedKeepers?.Count ?? 0;
        var duplicatesRemoved = originalCount - deduplicatedCount;

        return (originalCount, deduplicatedCount, duplicatesRemoved);
    }
}