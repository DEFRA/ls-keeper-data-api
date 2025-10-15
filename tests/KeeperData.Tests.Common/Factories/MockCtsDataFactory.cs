using AutoFixture;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
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

    public CtsCphHolding CreateMockHolding(string changeType, string? locType = null, DateTime? endDate = null)
    {
        _fixture.Customizations.Add(new CtsCphHoldingBuilder(changeType, locType, endDate));

        return _fixture.Create<CtsCphHolding>();
    }

    public CtsAgentOrKeeper CreateMockAgentOrKeeper(string changeType, string holdingIdentifier, int batchId = 1, DateTime? endDate = null)
    {
        _fixture.Customizations.Add(new CtsAgentOrKeeperBuilder(changeType, holdingIdentifier, batchId, endDate));

        return _fixture.Create<CtsAgentOrKeeper>();
    }

    public (List<CtsCphHolding> holdings, List<CtsAgentOrKeeper> agents, List<CtsAgentOrKeeper> keepers) CreateMockData(
        string changeType,
        int holdingCount,
        int agentCount,
        int keeperCount)
    {
        _fixture.Customizations.Add(new CtsCphHoldingBuilder(changeType));

        var holdings = _fixture.CreateMany<CtsCphHolding>(holdingCount).ToList();
        var holdingIdentifier = holdings.First().LID_FULL_IDENTIFIER;
        var batchId = holdings.First().BATCH_ID;

        _fixture.Customizations.Add(new CtsAgentOrKeeperBuilder(changeType, holdingIdentifier, batchId));

        var agents = _fixture.CreateMany<CtsAgentOrKeeper>(agentCount).ToList();
        var keepers = _fixture.CreateMany<CtsAgentOrKeeper>(keeperCount).ToList();

        return (holdings, agents, keepers);
    }
}