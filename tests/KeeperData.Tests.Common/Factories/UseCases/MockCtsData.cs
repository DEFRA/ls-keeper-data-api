using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Utilities;

namespace KeeperData.Tests.Common.Factories.UseCases;

public static class MockCtsData
{
    public static StringContent GetCtsHoldingsResponse(string holdingIdentifier) =>
        HttpContentUtility.CreateResponseContent(new List<CtsCphHolding>
        {
            new MockCtsDataFactory().CreateMockHolding(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: holdingIdentifier)
        });

    public static StringContent GetCtsAgentOrKeeperResponse(string holdingIdentifier) =>
        HttpContentUtility.CreateResponseContent(new List<CtsAgentOrKeeper>
        {
            new MockCtsDataFactory().CreateMockAgentOrKeeper(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: holdingIdentifier)
        });
}