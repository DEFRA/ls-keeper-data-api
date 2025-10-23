using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SamPartyMapper
{
    public static async Task<List<SamPartyDocument>> ToSilver(
        List<SamParty> parties,
        List<SamHerd> herds,
        CancellationToken cancellationToken)
    {
        return [];
    }
}