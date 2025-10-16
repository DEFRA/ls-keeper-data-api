using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Core.Tests.Unit.ApiContracts;

public class MockSamDataFactoryTests
{
    [Fact]
    public void GivenMockSamDataFactory_WhenCallingCreateMockHolding_ShouldProduceValidHoldingModel()
    {
        var factory = new MockSamDataFactory();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();

        var result = factory.CreateMockHolding(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: holdingIdentifier,
            endDate: DateTime.UtcNow.Date);

        result.Should().NotBeNull();

        result.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        result.BATCH_ID.Should().Be(1);

        result.CPH.Should().NotBeNull();
        result.CPH.Should().Be(holdingIdentifier);
    }

    [Fact]
    public void GivenMockSamDataFactory_WhenCallingCreateMockHolder_ShouldProduceValidHolderModel()
    {
        var factory = new MockSamDataFactory();
        var holdingIdentifier1 = CphGenerator.GenerateFormattedCph();
        var holdingIdentifier2 = CphGenerator.GenerateFormattedCph();

        var result = factory.CreateMockHolder(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifiers: [holdingIdentifier1, holdingIdentifier2]);

        result.Should().NotBeNull();

        result.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        result.BATCH_ID.Should().Be(1);

        result.CPHS.Should().NotBeNull();
        result.CphList.Should().Contain(holdingIdentifier1);
        result.CphList.Should().Contain(holdingIdentifier2);
    }

    [Fact]
    public void GivenMockSamDataFactory_WhenCallingCreateMockHerd_ShouldProduceValidHerdModel()
    {
        var factory = new MockSamDataFactory();
        var holdingIdentifier = CphGenerator.GenerateFormattedCph();
        var partyIds = PersonGenerator.GetPartyIds(3);

        var result = factory.CreateMockHerd(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            batchId: 1,
            holdingIdentifier: holdingIdentifier,
            partyIds);

        result.Should().NotBeNull();

        result.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        result.BATCH_ID.Should().Be(1);

        result.CPHH.Should().NotBeNull().And.Be(holdingIdentifier);
        result.OwnerPartyIdList.Should().HaveCountGreaterThan(0);
        result.KeeperPartyIdList.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void GivenMockSamDataFactory_WhenCallingCreateMockData_ShouldProduceValidModels()
    {
        var factory = new MockSamDataFactory();

        var (holdings, holders, herds, parties) = factory.CreateMockData(
            changeType: DataBridgeConstants.ChangeTypeInsert,
            holdingCount: 1,
            holderCount: 1,
            herdCount: 1,
            partyCount: 1);

        holdings.Should().NotBeNull().And.HaveCount(1);
        holders.Should().NotBeNull().And.HaveCount(1);
        herds.Should().NotBeNull().And.HaveCount(1);
        parties.Should().NotBeNull().And.HaveCount(1);

        var holding = holdings[0];

        holding.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        holding.CPH.Should().NotBeNull();
        holding.CPH.Length.Should().Be(11);

        var holder = holders[0];

        holder.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        holder.CphList.Should().NotBeNull().And.Contain(holding.CPH);

        var herd = herds[0];

        herd.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        herd.CPHH.Should().NotBeNull().And.Be(holding.CPH);
        herd.KeeperPartyIdList.Should().NotBeNull().And.HaveCount(1);
        herd.OwnerPartyIdList.Should().NotBeNull().And.HaveCount(1);

        var party = parties[0];
        party.CHANGE_TYPE.Should().Be(DataBridgeConstants.ChangeTypeInsert);
        party.PARTY_ID.Should().NotBeNull().And.Be(herd.KeeperPartyIdList[0]);
    }
}
