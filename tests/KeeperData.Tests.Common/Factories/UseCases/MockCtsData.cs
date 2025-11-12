using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Utilities;

namespace KeeperData.Tests.Common.Factories.UseCases;

public static class MockCtsData
{
    private static readonly Fixture s_fixture = new();

    public static DataBridgeResponse<CtsCphHolding> GetCtsHoldingsDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<CtsCphHolding>(count)]
        };            

    public static StringContent GetCtsHoldingsStringContentResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<CtsCphHolding>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<CtsCphHolding>(top)]
            });

    public static StringContent GetCtsHoldingsStringContentResponse(string holdingIdentifier) =>
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

    public static DataBridgeResponse<CtsAgentOrKeeper> GetCtsAgentOrKeeperDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<CtsAgentOrKeeper>(count)]
        };

    public static StringContent GetCtsAgentOrKeeperStringContentResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<CtsAgentOrKeeper>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<CtsAgentOrKeeper>(top)]
            });

    public static StringContent GetCtsAgentOrKeeperStringContentResponse(string holdingIdentifier) =>
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