using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Source;

namespace KeeperData.Application.Orchestration.Sam.Mappings;

public static class SamHolderMapper
{
    public static SamPartyDocument ToSilver(SamCphHolder raw)
    {
        return new SamPartyDocument
        {   
            PartyId = raw.PARTY_ID
        };
    }
}
