using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Extensions;

namespace KeeperData.Tests.Common.Mappings;

public static class VerifyCtsHoldingMappings
{
    public static void VerifyMapping_From_CtsCphHolding_To_CtsHoldingDocument(CtsCphHolding source, CtsHoldingDocument target)
    {
        source.Should().NotBeNull();
        target.Should().NotBeNull();

        target.Id.Should().BeNull();
        target.LastUpdatedBatchId.Should().Be(source.BATCH_ID);
        target.Deleted.Should().BeFalse();

        target.CountyParishHoldingNumber.Should().Be(source.LID_FULL_IDENTIFIER);
        target.AlternativeHoldingIdentifier.Should().BeNull();

        target.CphTypeIdentifier.Should().Be(source.LTY_LOC_TYPE);
        target.LocationName.Should().Be(source.ADR_NAME);

        target.HoldingStartDate.Should().Be(source.LOC_EFFECTIVE_FROM);
        target.HoldingEndDate.Should().Be(source.LOC_EFFECTIVE_TO);

        var expectedStatus = (source.IsDeleted ?? false)
            ? HoldingStatusType.Inactive.GetDescription()
            : HoldingStatusType.Active.GetDescription();
        target.HoldingStatus.Should().Be(expectedStatus);

        target.PremiseActivityTypeId.Should().BeNull();
        target.PremiseActivityTypeCode.Should().BeNull();
        target.PremiseTypeIdentifier.Should().BeNull();
        target.PremiseTypeCode.Should().BeNull();

        // Location
        target.Location.Should().NotBeNull();
        target.Location.IdentifierId.Should().NotBeNullOrWhiteSpace();
        target.Location.Easting.Should().BeNull();
        target.Location.Northing.Should().BeNull();
        target.Location.OsMapReference.Should().BeNull();

        // Address
        var address = target.Location.Address;
        address.Should().NotBeNull();
        address.IdentifierId.Should().NotBeNullOrWhiteSpace();
        address.AddressLine.Should().Be(source.ADR_ADDRESS_2);
        address.AddressLocality.Should().Be(source.ADR_ADDRESS_3);
        address.AddressStreet.Should().Be(source.ADR_ADDRESS_4);
        address.AddressTown.Should().Be(source.ADR_ADDRESS_5);
        address.AddressPostCode.Should().Be(source.ADR_POST_CODE);
        address.CountryIdentifier.Should().BeNull();
        address.CountryCode.Should().BeNull();
        address.UniquePropertyReferenceNumber.Should().Be(source.LOC_MAP_REFERENCE);

        // Communication
        var comms = target.Communication;
        comms.Should().NotBeNull();
        comms.IdentifierId.Should().NotBeNullOrWhiteSpace();
        comms.Email.Should().BeNull();
        comms.Mobile.Should().Be(source.LOC_MOBILE_NUMBER);
        comms.Landline.Should().Be(source.LOC_TEL_NUMBER);
    }
}