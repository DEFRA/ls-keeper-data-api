using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.SpecimenBuilders;

namespace KeeperData.Tests.Common.Factories;

/// <summary>
/// Factory to be used to create mock objects for <see cref="IDataBridgeClient"/> to be used by all tests.
/// </summary>
public class MockCtsDataFactory
{
    private readonly Fixture _fixture;

    public MockCtsDataFactory()
    {
        _fixture = new Fixture();
    }

    public CtsCphHolding CreateMockHolding(
        string changeType,
        int batchId,
        string holdingIdentifier,
        string? locType = null,
        DateTime? endDate = null)
    {
        _fixture.Customizations.Add(new CtsCphHoldingBuilder(
            changeType,
            batchId,
            holdingIdentifier,
            locType,
            endDate));

        return _fixture.Create<CtsCphHolding>();
    }

    public CtsAgentOrKeeper CreateMockAgentOrKeeper(
        string changeType,
        int batchId,
        string holdingIdentifier,
        DateTime? endDate = null)
    {
        _fixture.Customizations.Add(new CtsAgentOrKeeperBuilder(
            changeType,
            batchId,
            holdingIdentifier,
            endDate));

        return _fixture.Create<CtsAgentOrKeeper>();
    }

    public (List<CtsCphHolding> holdings, List<CtsAgentOrKeeper> agents, List<CtsAgentOrKeeper> keepers) CreateMockData(
        string changeType,
        int holdingCount,
        int agentCount,
        int keeperCount)
    {
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var batchId = 1;

        _fixture.Customizations.Add(new CtsCphHoldingBuilder(changeType, batchId, holdingIdentifier));

        var holdings = _fixture.CreateMany<CtsCphHolding>(holdingCount).ToList();
        
        _fixture.Customizations.Add(new CtsAgentOrKeeperBuilder(changeType, batchId, holdingIdentifier));

        var agents = _fixture.CreateMany<CtsAgentOrKeeper>(agentCount).ToList();
        var keepers = _fixture.CreateMany<CtsAgentOrKeeper>(keeperCount).ToList();

        return (holdings, agents, keepers);
    }
}