using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.SpecimenBuilders;

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
        List<string> holdingIdentifiers)
    {
        _fixture.Customizations.Add(new SamCphHolderBuilder(
            changeType,
            batchId,
            holdingIdentifiers));

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

        var holdings = _fixture.CreateMany<SamCphHolding>(holdingCount).ToList();

        _fixture.Customizations.Add(new SamCphHolderBuilder(changeType, batchId, [holdingIdentifier]));

        var holders = _fixture.CreateMany<SamCphHolder>(holderCount).ToList();

        _fixture.Customizations.Add(new SamHerdBuilder(changeType, batchId, holdingIdentifier, partyIds));

        var herds = _fixture.CreateMany<SamHerd>(herdCount).ToList();

        _fixture.Customizations.Add(new SamPartyBuilder(changeType, batchId, partyIds));

        var parties = _fixture.CreateMany<SamParty>(partyCount).ToList();

        return (holdingIdentifier, holdings, holders, herds, parties);
    }

    private static Dictionary<(string animalSpeciesCode, string animalProductionUsageCode), List<string>> GetHerdSpeciesPartyAssociations(int herdCount, int partyCount)
    {
        var herdSpeciesParties = new Dictionary<(string animalSpeciesCode, string animalProductionUsageCode), List<string>>();

        var animalSpeciesAndProductionUsageCodes = Enumerable.Range(1, herdCount)
            .Select(i => FacilityGenerator.GenerateAnimalSpeciesAndProductionUsageCodes(allowNulls: false))
            .ToList();

        foreach (var item in animalSpeciesAndProductionUsageCodes)
        {
            herdSpeciesParties.Add(
                item,
                PersonGenerator.GetPartyIds(partyCount));
        }

        return herdSpeciesParties;
    }
}