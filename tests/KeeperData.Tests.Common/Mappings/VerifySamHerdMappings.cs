using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Tests.Common.Mappings;

public static class VerifySamHerdMappings
{
    public static void VerifyMapping_From_SamHerd_To_SamHerdDocument(SamHerd source, SamHerdDocument target)
    {
        source.Should().NotBeNull();
        target.Should().NotBeNull();

        target.Id.Should().BeNull();

        target.LastUpdatedBatchId.Should().Be(source.BATCH_ID);
        target.Deleted.Should().BeFalse();

        target.Herdmark.Should().Be(source.HERDMARK);
        target.CountyParishHoldingHerd.Should().Be(source.CPHH);
        target.CountyParishHoldingNumber.Should().Be(source.CPHH.CphhToCph());

        target.SpeciesTypeId.Should().NotBeNullOrWhiteSpace();
        target.SpeciesTypeCode.Should().Be(source.AnimalSpeciesCodeUnwrapped);

        target.ProductionUsageId.Should().NotBeNullOrWhiteSpace();
        target.ProductionUsageCode.Should().Be(source.AnimalPurposeCodeUnwrapped);

        target.ProductionTypeId.Should().BeNull();
        target.ProductionTypeCode.Should().BeNull();

        target.DiseaseType.Should().Be(source.DISEASE_TYPE);
        target.Interval.Should().Be(source.INTERVALS);
        target.IntervalUnitOfTime.Should().Be(source.INTERVAL_UNIT_OF_TIME);

        target.MovementRestrictionReasonCode.Should().Be(source.MOVEMENT_RSTRCTN_RSN_CODE);

        target.GroupMarkStartDate.Should().Be(source.ANIMAL_GROUP_ID_MCH_FRM_DAT);
        target.GroupMarkEndDate.Should().Be(source.ANIMAL_GROUP_ID_MCH_TO_DAT);

        target.KeeperPartyIdList.Should().NotBeNull();
        target.KeeperPartyIdList.Should().HaveCount(source.KeeperPartyIdList.Count);

        target.OwnerPartyIdList.Should().NotBeNull();
        target.OwnerPartyIdList.Should().HaveCount(source.OwnerPartyIdList.Count);
    }
}