using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Utilities;

namespace KeeperData.Tests.Common.Factories.UseCases;

public static class MockSamData
{
    private static readonly Fixture s_fixture = new();

    public static DataBridgeResponse<SamCphHolding> GetSamHoldingsDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<SamCphHolding>(count)]
        };

    public static DataBridgeResponse<SamScanHoldingIdentifier> GetSamHoldingsScanIdentifierDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<SamScanHoldingIdentifier>(count)]
        };

    public static StringContent GetSamHoldingsStringContentResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamCphHolding>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<SamCphHolding>(top)]
            });

    public static StringContent GetSamHoldingsStringContentResponse(string holdingIdentifier) =>
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

    public static DataBridgeResponse<SamCphHolder> GetSamHolderDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<SamCphHolder>(count)]
        };

    public static DataBridgeResponse<SamScanHolderIdentifier> GetSamHolderScanIdentifierDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<SamScanHolderIdentifier>(count)]
        };

    public static StringContent GetSamHolderStringContentResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamCphHolder>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<SamCphHolder>(top)]
            });

    public static StringContent GetSamHolderStringContentResponse(string partyId, List<string> holdingIdentifiers) =>
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

    public static DataBridgeResponse<SamHerd> GetSamHerdsDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<SamHerd>(count)]
        };

    public static DataBridgeResponse<SamScanHerdIdentifier> GetSamHerdsScanIdentifierDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<SamScanHerdIdentifier>(count)]
        };

    public static StringContent GetSamHerdsStringContentResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamHerd>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<SamHerd>(top)]
            });

    public static StringContent GetSamHerdsStringContentResponse(string holdingIdentifier, string partyId) =>
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

    public static StringContent GetSamPartyStringContentResponse(string partyId) =>
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

    public static DataBridgeResponse<SamParty> GetSamPartiesDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<SamParty>(count)]
        };

    public static DataBridgeResponse<SamScanPartyIdentifier> GetSamPartiesScanIdentifierDataBridgeResponse(int top, int count, int totalCount) =>
        new()
        {
            CollectionName = "collection",
            Top = top,
            Skip = top,
            Count = count,
            TotalCount = totalCount,
            Data = [.. s_fixture.CreateMany<SamScanPartyIdentifier>(count)]
        };

    public static StringContent GetSamPartiesStringContentResponse(int top, int skip) =>
        HttpContentUtility.CreateResponseContent(
            new DataBridgeResponse<SamParty>
            {
                CollectionName = "collection",
                Top = top,
                Skip = skip,
                Data = [.. s_fixture.CreateMany<SamParty>(top)]
            });

    public static StringContent GetSamPartiesStringContentResponse(string partyId) =>
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