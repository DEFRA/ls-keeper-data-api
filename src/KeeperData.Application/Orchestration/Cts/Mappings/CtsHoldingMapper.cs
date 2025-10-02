using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Source;

namespace KeeperData.Application.Orchestration.Cts.Mappings;

public static class CtsHoldingMapper
{
    public static CtsHoldingDocument ToSilver(CtsCphHolding raw)
    {
        return new CtsHoldingDocument
        {
            CountyParishHoldingNumber = raw.LID_FULL_IDENTIFIER
        };
    }
}