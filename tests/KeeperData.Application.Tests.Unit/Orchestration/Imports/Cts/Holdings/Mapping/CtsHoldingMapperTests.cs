using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Cts.Holdings.Mappings;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using KeeperData.Tests.Common.Mappings;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Cts.Holdings.Mapping;

public class CtsHoldingMapperTests
{
    [Fact]
    public void GivenNullableRawHoldings_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = CtsHoldingMapper.ToSilver(
            DateTime.UtcNow,
            null!);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Fact]
    public void GivenEmptyRawHoldings_WhenCallingToSilver_ShouldReturnEmptyList()
    {
        var results = CtsHoldingMapper.ToSilver(
            DateTime.UtcNow,
            []);

        results.Should().NotBeNull();
        results.Count.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void GivenRawHoldings_WhenCallingToSilver_ShouldReturnPopulatedList(int quantity)
    {
        var records = GenerateCtsCphHolding(quantity);

        var results = CtsHoldingMapper.ToSilver(
            DateTime.UtcNow,
            records);

        results.Should().NotBeNull();
        results.Count.Should().Be(quantity);

        for (var i = 0; i < quantity; i++)
        {
            VerifyCtsHoldingMappings.VerifyMapping_From_CtsCphHolding_To_CtsHoldingDocument(records[i], results[i]);
        }
    }

    private static List<CtsCphHolding> GenerateCtsCphHolding(int quantity)
    {
        var records = new List<CtsCphHolding>();
        var factory = new MockCtsRawDataFactory();
        for (var i = 0; i < quantity; i++)
        {
            records.Add(factory.CreateMockHolding(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: CphGenerator.GenerateFormattedCph(),
                locType: "AG",
                endDate: DateTime.UtcNow.Date));
        }
        return records;
    }
}