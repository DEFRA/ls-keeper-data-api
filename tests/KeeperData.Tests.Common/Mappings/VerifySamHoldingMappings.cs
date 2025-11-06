using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Tests.Common.Mappings;

public static class VerifySamHoldingMappings
{
    public static void VerifyMapping_From_SamCphHolding_To_SamHoldingDocument(SamCphHolding source, SamHoldingDocument target)
    {
        var addressLine = AddressFormatters.FormatAddressRange(
                            source.SAON_START_NUMBER, source.SAON_START_NUMBER_SUFFIX,
                            source.SAON_END_NUMBER, source.SAON_END_NUMBER_SUFFIX,
                            source.PAON_START_NUMBER, source.PAON_START_NUMBER_SUFFIX,
                            source.PAON_END_NUMBER, source.PAON_END_NUMBER_SUFFIX,
                            source.SAON_DESCRIPTION, source.PAON_DESCRIPTION);

        source.Should().NotBeNull();
        target.Should().NotBeNull();

        target.Id.Should().BeNull();
        target.LastUpdatedBatchId.Should().Be(source.BATCH_ID);
        target.Deleted.Should().BeFalse();

        target.CountyParishHoldingNumber.Should().Be(source.CPH);
        target.AlternativeHoldingIdentifier.Should().BeNull();

        target.CphTypeIdentifier.Should().Be(source.CPH_TYPE);
        target.LocationName.Should().Be(source.FEATURE_NAME);

        target.HoldingStartDate.Should().Be(source.FEATURE_ADDRESS_FROM_DATE);
        target.HoldingEndDate.Should().Be(source.FEATURE_ADDRESS_TO_DATE);

        var expectedStatus = source.FEATURE_ADDRESS_TO_DATE.HasValue && source.FEATURE_ADDRESS_TO_DATE != default
            ? HoldingStatusType.Inactive.ToString()
            : HoldingStatusType.Active.ToString();
        target.HoldingStatus.Should().Be(expectedStatus);

        target.PremiseActivityTypeId.Should().NotBeNullOrWhiteSpace();
        target.PremiseActivityTypeCode.Should().Be(source.FACILITY_BUSINSS_ACTVTY_CODE);
        target.PremiseTypeIdentifier.Should().NotBeNullOrWhiteSpace();
        target.PremiseTypeCode.Should().Be(source.FACILITY_TYPE_CODE);

        // Location
        target.Location.Should().NotBeNull();
        target.Location.IdentifierId.Should().NotBeNullOrWhiteSpace();
        target.Location.Easting.Should().Be(source.EASTING);
        target.Location.Northing.Should().Be(source.NORTHING);
        target.Location.OsMapReference.Should().Be(source.OS_MAP_REFERENCE);

        // Address
        var address = target.Location.Address;
        address.Should().NotBeNull();
        address.IdentifierId.Should().NotBeNullOrWhiteSpace();
        address.AddressLine.Should().Be(addressLine);
        address.AddressLocality.Should().Be(source.LOCALITY);
        address.AddressStreet.Should().Be(source.STREET);
        address.AddressTown.Should().Be(source.TOWN);
        address.AddressPostCode.Should().Be(source.POSTCODE);
        address.CountryIdentifier.Should().NotBeNullOrWhiteSpace();
        address.CountryCode.Should().Be(source.COUNTRY_CODE);
        address.UniquePropertyReferenceNumber.Should().Be(source.UDPRN);

        // Communication
        var comms = target.Communication;
        comms.Should().NotBeNull();
        comms.IdentifierId.Should().NotBeNullOrWhiteSpace();
        comms.Email.Should().BeNull();
        comms.Mobile.Should().BeNull();
        comms.Landline.Should().BeNull();

        // GroupMarks
        target.GroupMarks.Should().NotBeNull();
        target.GroupMarks.Should().BeEmpty();
    }
}