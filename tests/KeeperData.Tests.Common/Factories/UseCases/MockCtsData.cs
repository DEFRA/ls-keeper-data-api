using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Utilities;

namespace KeeperData.Tests.Common.Factories.UseCases;

public static class MockCtsData
{
    private static readonly Fixture s_fixture = new();

    public static StringContent GetCtsHoldingsResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<CtsCphHolding>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<CtsCphHolding>(top)]
            });

    public static StringContent GetCtsHoldingsResponse(string holdingIdentifier) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<CtsCphHolding>
            {
                CollectionName = "collection",
                Data =
                [
                    new MockCtsRawDataFactory().CreateMockHolding(
                        changeType: DataBridgeConstants.ChangeTypeInsert,
                        batchId: 1,
                        holdingIdentifier: holdingIdentifier)
                ]
            });

    public static StringContent GetCtsAgentOrKeeperResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<CtsAgentOrKeeper>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<CtsAgentOrKeeper>(top)]
            });

    public static StringContent GetCtsAgentOrKeeperResponse(string holdingIdentifier) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<CtsAgentOrKeeper>
            {
                CollectionName = "collection",
                Data =
                [
                    new MockCtsRawDataFactory().CreateMockAgentOrKeeper(
                        changeType: DataBridgeConstants.ChangeTypeInsert,
                        batchId: 1,
                        holdingIdentifier: holdingIdentifier)
                ]
            });
}