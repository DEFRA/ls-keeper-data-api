using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Cts.Inserts.Mappings;

public static class CtsHoldingMapper
{
    public static CtsHoldingDocument ToSilver(CtsCphHolding raw)
    {
        return new CtsHoldingDocument
        {
            LastUpdatedBatchId = raw.BATCH_ID,
            CountyParishHoldingNumber = raw.LID_FULL_IDENTIFIER
        };
    }
}