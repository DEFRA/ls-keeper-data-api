using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Inserts.Mappings;

public static class SamHoldingMapper
{
    public static SamHoldingDocument ToSilver(SamCphHolding raw)
    {
        return new SamHoldingDocument
        {
            CountyParishHoldingNumber = raw.CPH
        };
    }
}