using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Infrastructure;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.SpecimenBuilders;
using System.Text.Json;

namespace KeeperData.Tests.Common.Factories;

/// <summary>
/// Factory to be used to create mock objects for <see cref="IDataBridgeClient"/> to be used by all tests.
/// </summary>
public class MockSamRawDataFactory
{
    private readonly Fixture _fixture;

    public MockSamRawDataFactory()
    {
        _fixture = new Fixture();
    }

    public SamCphHolding CreateMockHolding(
        string changeType,
        int batchId,
        string holdingIdentifier,
        DateTime? endDate = null)
    {
        _fixture.Customizations.Add(new SamCphHoldingBuilder(
            changeType,
            batchId,
            holdingIdentifier,
            endDate));

        return _fixture.Create<SamCphHolding>();
    }

    public SamCphHolder CreateMockHolder(
        string changeType,
        int batchId,
        List<string> holdingIdentifiers,
        string? partyId = null)
    {
        _fixture.Customizations.Add(new SamCphHolderBuilder(
            changeType,
            batchId,
            holdingIdentifiers,
            partyId));

        return _fixture.Create<SamCphHolder>();
    }

    public SamHerd CreateMockHerd(
        string changeType,
        int batchId,
        string holdingIdentifier,
        List<string> partyIds)
    {
        _fixture.Customizations.Add(new SamHerdBuilder(
            changeType,
            batchId,
            holdingIdentifier,
            partyIds));

        return _fixture.Create<SamHerd>();
    }

    public SamParty CreateMockParty(
        string changeType,
        int batchId,
        List<string> partyIds)
    {
        _fixture.Customizations.Add(new SamPartyBuilder(
            changeType,
            batchId,
            partyIds));

        return _fixture.Create<SamParty>();
    }

    public (string holdingIdentifier, List<SamCphHolding> holdings, List<SamCphHolder> holders, List<SamHerd> herds, List<SamParty> parties) CreateMockData(
        string changeType,
        int holdingCount,
        int holderCount,
        int herdCount,
        int partyCount)
    {
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var batchId = 1;

        var partyIds = PersonGenerator.GetPartyIds(partyCount);

        _fixture.Customizations.Add(new SamCphHoldingBuilder(changeType, batchId, holdingIdentifier));

        var holdings = RoundTripViaJson(_fixture.CreateMany<SamCphHolding>(holdingCount).ToList());

        _fixture.Customizations.Add(new SamCphHolderBuilder(changeType, batchId, [holdingIdentifier]));

        var holders = RoundTripViaJson(_fixture.CreateMany<SamCphHolder>(holderCount).ToList());

        _fixture.Customizations.Add(new SamHerdBuilder(changeType, batchId, holdingIdentifier, partyIds));

        var herds = RoundTripViaJson(_fixture.CreateMany<SamHerd>(herdCount).ToList());

        _fixture.Customizations.Add(new SamPartyBuilder(changeType, batchId, partyIds));

        var parties = RoundTripViaJson(_fixture.CreateMany<SamParty>(partyCount).ToList());

        return (holdingIdentifier, holdings, holders, herds, parties);
    }

    private static T RoundTripViaJson<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json, JsonDefaults.DefaultOptionsWithDataBridgeApiSupport)!;
    }
}