using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.SpecimenBuilders;

namespace KeeperData.Tests.Common.Factories;

/// <summary>
/// Factory to be used to create mock objects for <see cref="IDataBridgeClient"/> to be used by all tests.
/// </summary>
public class MockSamDataFactory
{
    private readonly Fixture _fixture;

    public MockSamDataFactory()
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
        string holdingIdentifier)
    {
        _fixture.Customizations.Add(new SamCphHolderBuilder(
            changeType,
            batchId,
            holdingIdentifier));

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
        string partyId)
    {
        _fixture.Customizations.Add(new SamPartyBuilder(
            changeType,
            batchId,
            [partyId]));

        return _fixture.Create<SamParty>();
    }

    public (List<SamCphHolding> holdings, List<SamCphHolder> holders, List<SamHerd> herds, List<SamParty> parties) CreateMockData(
        string changeType,
        int holdingCount,
        int holderCount,
        int herdCount,
        int partyCount)
    {
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var batchId = 1;

        var partyIds = Enumerable.Range(1, partyCount)
            .Select(i => $"C{i:D6}")
            .ToList();

        _fixture.Customizations.Add(new SamCphHoldingBuilder(changeType, batchId, holdingIdentifier));

        var holdings = _fixture.CreateMany<SamCphHolding>(holdingCount).ToList();

        _fixture.Customizations.Add(new SamCphHolderBuilder(changeType, batchId, holdingIdentifier));

        var holders = _fixture.CreateMany<SamCphHolder>(holderCount).ToList();

        _fixture.Customizations.Add(new SamHerdBuilder(changeType, batchId, holdingIdentifier, partyIds));

        var herds = _fixture.CreateMany<SamHerd>(herdCount).ToList();

        _fixture.Customizations.Add(new SamPartyBuilder(changeType, batchId, partyIds));

        var parties = _fixture.CreateMany<SamParty>(partyCount).ToList();

        return (holdings, holders, herds, parties);
    }
}
