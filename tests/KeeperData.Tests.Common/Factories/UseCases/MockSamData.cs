using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Utilities;

namespace KeeperData.Tests.Common.Factories.UseCases;

public static class MockSamData
{
    private static readonly Fixture s_fixture = new();

    public static StringContent GetSamHoldingsResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamCphHolding>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<SamCphHolding>(top)]
            });

    public static StringContent GetSamHoldingsResponse(string holdingIdentifier) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamCphHolding>
            {
                CollectionName = "collection",
                Data =
                [
                    new MockSamRawDataFactory().CreateMockHolding(
                        changeType: DataBridgeConstants.ChangeTypeInsert,
                        batchId: 1,
                        holdingIdentifier: holdingIdentifier)
                ]
            });

    public static StringContent GetSamHolderResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamCphHolder>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<SamCphHolder>(top)]
            });

    public static StringContent GetSamHolderResponse(string partyId, List<string> holdingIdentifiers) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamCphHolder>
            {
                CollectionName = "collection",
                Data =
                [
                    new MockSamRawDataFactory().CreateMockHolder(
                        changeType: DataBridgeConstants.ChangeTypeInsert,
                        batchId: 1,
                        holdingIdentifiers: holdingIdentifiers,
                        partyId: partyId)
                ]
            });

    public static StringContent GetSamHerdsResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamHerd>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<SamHerd>(top)]
            });

    public static StringContent GetSamHerdsResponse(string holdingIdentifier, string partyId) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamHerd>
            {
                CollectionName = "collection",
                Data =
                [
                    new MockSamRawDataFactory().CreateMockHerd(
                        changeType: DataBridgeConstants.ChangeTypeInsert,
                        batchId: 1,
                        holdingIdentifier: holdingIdentifier,
                        partyIds: [partyId])
                ]
            });

    public static StringContent GetSamPartyResponse(string partyId) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamParty>
            {
                CollectionName = "collection",
                Data =
                [
                    new MockSamRawDataFactory().CreateMockParty(
                        changeType: DataBridgeConstants.ChangeTypeInsert,
                        batchId: 1,
                        partyIds: [partyId])
                ]
            });

    public static StringContent GetSamPartiesResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamParty>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<SamParty>(top)]
            });

    public static StringContent GetSamPartiesResponse(string partyId) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamParty>
            {
                CollectionName = "collection",
                Data =
                [
                    new MockSamRawDataFactory().CreateMockParty(
                        changeType: DataBridgeConstants.ChangeTypeInsert,
                        batchId: 1,
                        partyIds: [partyId])
                ]
            });
}