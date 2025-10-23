using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SamHoldingMapper
{
    public static async Task<List<SamHoldingDocument>> ToSilver(
        List<SamCphHolding> rawHoldings,
        CancellationToken cancellationToken)
    {
        var result = rawHoldings?
            .Where(x => x.CPH != null)
            .Select(h => new SamHoldingDocument()
            {
                // Id - Leave to support upsert assigning Id

                LastUpdatedBatchId = h.BATCH_ID,
                Deleted = h.IsDeleted ?? false
            });

        return result?.ToList() ?? [];
    }
}