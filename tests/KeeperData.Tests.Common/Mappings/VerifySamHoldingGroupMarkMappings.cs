using FluentAssertions;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Tests.Common.Mappings;

public static class VerifySamHoldingGroupMarkMappings
{
    public static void VerifyMappingEnrichingWithGroupMarks(List<SamHerdDocument> herds, SamHoldingDocument holding)
    {
        herds.Should().NotBeNull();
        holding.Should().NotBeNull();

        holding.GroupMarks.Should().NotBeNull();
        holding.GroupMarks.Count.Should().Be(herds.Count);

        for (var i = 0; i < herds.Count; i++)
        {
            var source = herds[i];
            var target = holding.GroupMarks.FirstOrDefault(x => x.GroupMark == source.Herdmark);

            source.Should().NotBeNull();
            target.Should().NotBeNull();

            target.GroupMark.Should().Be(source.Herdmark);
            target.CountyParishHoldingNumber.Should().Be(source.CountyParishHoldingHerd);

            target.SpeciesTypeId.Should().NotBeNullOrWhiteSpace();
            target.SpeciesTypeCode.Should().Be(source.SpeciesTypeCode);

            target.ProductionUsageId.Should().NotBeNullOrWhiteSpace();
            target.ProductionUsageCode.Should().Be(source.ProductionUsageCode);

            target.ProductionTypeId.Should().BeNull();
            target.ProductionTypeCode.Should().BeNull();

            target.TbTestingIntervalId.Should().Be(TestingIntervalFormatters.FormatTbTestingInterval(
                    source.Interval,
                    source.IntervalUnitOfTime));

            target.GroupMarkStartDate.Should().Be(source.GroupMarkStartDate);
            target.GroupMarkEndDate.Should().Be(source.GroupMarkEndDate);
        }
    }
}