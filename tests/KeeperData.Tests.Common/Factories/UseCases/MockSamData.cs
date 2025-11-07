using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Utilities;

namespace KeeperData.Tests.Common.Factories.UseCases;

public static class MockSamData
{
    public static StringContent GetSamHoldingsResponse(string holdingIdentifier) =>
        HttpContentUtility.CreateResponseContent(new List<SamCphHolding>
        {
            new MockSamRawDataFactory().CreateMockHolding(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: holdingIdentifier)
        });
    
    public static StringContent GetSamHolderResponse(string partyId, List<string> holdingIdentifiers) =>
        HttpContentUtility.CreateResponseContent(new List<SamCphHolder>
        {
            new MockSamRawDataFactory().CreateMockHolder(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifiers: holdingIdentifiers,
                partyId: partyId)
        });

    public static StringContent GetSamHoldersResponse(string holdingIdentifier) =>
        HttpContentUtility.CreateResponseContent(new List<SamCphHolder>
        {
            new MockSamRawDataFactory().CreateMockHolder(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifiers: [holdingIdentifier])
        });

    public static StringContent GetSamHerdsResponse(string holdingIdentifier, string partyId) =>
        HttpContentUtility.CreateResponseContent(new List<SamHerd>
        {
            new MockSamRawDataFactory().CreateMockHerd(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: holdingIdentifier,
                partyIds: [partyId])
        });

    public static StringContent GetSamPartyResponse(string partyId) =>
        HttpContentUtility.CreateResponseContent(new MockSamRawDataFactory().CreateMockParty(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            partyIds: [partyId]));

    public static StringContent GetSamPartiesResponse(string partyId) =>
        HttpContentUtility.CreateResponseContent(new List<SamParty>
        {
            new MockSamRawDataFactory().CreateMockParty(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                partyIds: [partyId])
        });
}