using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Core.Tests.Unit.ApiContracts;

public class MockCtsDataFactoryTests
{
    [Fact]
    public void GivenMockCtsDataFactory_WhenCallingCreateMockHolding_ShouldProduceValidHoldingModel()
    {
        var factory = new MockCtsDataFactory();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var result = factory.CreateMockHolding(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: holdingIdentifier,
            locType: "AG",
            endDate: DateTime.UtcNow.Date);

        result.Should().NotBeNull();

        result.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        result.BATCH_ID.Should().Be(1);

        result.LID_FULL_IDENTIFIER.Should().NotBeNull();
        result.LID_FULL_IDENTIFIER.Should().Be(holdingIdentifier);

        result.LTY_LOC_TYPE.Should().Be("AG");
        result.LOC_EFFECTIVE_TO.Should().NotBeNull();
    }

    [Fact]
    public void GivenMockCtsDataFactory_WhenCallingCreateMockAgentOrKeeper_ShouldProduceValidPartyModel()
    {
        var factory = new MockCtsDataFactory();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var result = factory.CreateMockAgentOrKeeper(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: holdingIdentifier,
            endDate: DateTime.UtcNow.Date);

        result.Should().NotBeNull();

        result.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        result.BATCH_ID.Should().Be(1);

        result.LID_FULL_IDENTIFIER.Should().NotBeNull().And.Be(holdingIdentifier);
        result.LPR_EFFECTIVE_TO_DATE.Should().NotBeNull();
    }

    [Fact]
    public void GivenMockCtsDataFactory_WhenCallingCreateMockData_ShouldProduceValidModels()
    {
        var factory = new MockCtsDataFactory();

        var (_, holdings, agents, keepers) = factory.CreateMockData(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            holdingCount: 1,
            agentCount: 1,
            keeperCount: 1);

        holdings.Should().NotBeNull().And.HaveCount(1);
        agents.Should().NotBeNull().And.HaveCount(1);
        keepers.Should().NotBeNull().And.HaveCount(1);

        var holder = holdings[0];

        holder.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        holder.LID_FULL_IDENTIFIER.Should().NotBeNull();
        holder.LID_FULL_IDENTIFIER.Length.Should().Be(11);

        var agent = agents[0];

        agent.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        agent.LID_FULL_IDENTIFIER.Should().NotBeNull().And.Be(holder.LID_FULL_IDENTIFIER);

        var keeper = keepers[0];

        keeper.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        keeper.LID_FULL_IDENTIFIER.Should().NotBeNull().And.Be(holder.LID_FULL_IDENTIFIER);
    }
}