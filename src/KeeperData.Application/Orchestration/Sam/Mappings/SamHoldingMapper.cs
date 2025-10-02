using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Source;

namespace KeeperData.Application.Orchestration.Sam.Mappings;

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