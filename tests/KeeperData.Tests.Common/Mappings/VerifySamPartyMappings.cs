using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Parties.Formatters;
using KeeperData.Core.Domain.Sites.Formatters;

namespace KeeperData.Tests.Common.Mappings;

public static class VerifySamPartyMappings
{
    public static void VerifyMapping_From_SamParty_To_SamPartyDocument(SamParty source, SamPartyDocument target)
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

        target.PartyId.Should().Be(source.PARTY_ID);

        var expectedPartyTypeId = !string.IsNullOrWhiteSpace(source.ORGANISATION_NAME)
            ? PartyType.Business.ToString()
            : PartyType.Person.ToString();
        target.PartyTypeId.Should().Be(expectedPartyTypeId);

        target.PartyFullName.Should().Be(PartyNameFormatters.FormatPartyFullName(
            source.ORGANISATION_NAME,
            source.PERSON_TITLE,
            source.PERSON_GIVEN_NAME,
            source.PERSON_GIVEN_NAME2,
            source.PERSON_INITIALS,
            source.PERSON_FAMILY_NAME));

        target.PartyTitleTypeIdentifier.Should().Be(source.PERSON_TITLE);
        target.PartyFirstName.Should().Be(PartyNameFormatters.FormatPartyFirstName(
            source.PERSON_GIVEN_NAME,
            source.PERSON_GIVEN_NAME2));
        target.PartyInitials.Should().Be(source.PERSON_INITIALS);
        target.PartyLastName.Should().Be(source.PERSON_FAMILY_NAME);

        // Address
        var address = target.Address;
        address.Should().NotBeNull();
        address.IdentifierId.Should().NotBeNullOrWhiteSpace();
        address.AddressLine.Should().Be(addressLine);
        address.AddressLocality.Should().Be(source.LOCALITY);
        address.AddressStreet.Should().Be(source.STREET);
        address.AddressTown.Should().Be(source.TOWN);
        address.AddressPostCode.Should().Be(source.POSTCODE);
        address.CountrySubDivision.Should().Be(source.UK_INTERNAL_CODE);
        address.CountryIdentifier.Should().NotBeNullOrWhiteSpace();
        address.CountryCode.Should().Be(source.COUNTRY_CODE);
        address.UniquePropertyReferenceNumber.Should().Be(source.UDPRN);

        // Communication
        var comms = target.Communication;
        comms.Should().NotBeNull();
        comms.IdentifierId.Should().NotBeNullOrWhiteSpace();
        comms.Email.Should().Be(source.INTERNET_EMAIL_ADDRESS);
        comms.Mobile.Should().Be(source.MOBILE_NUMBER);
        comms.Landline.Should().Be(source.TELEPHONE_NUMBER);

        // Roles
        var sourceRoleList = source.ROLES?.Split(",")
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .ToArray() ?? [];

        target.Roles.Should().NotBeNull().And.HaveCount(sourceRoleList.Length);

        for (var i = 0; i < sourceRoleList.Length; i++)
        {
            var roleNameToLookup = sourceRoleList[i];

            var role = target.Roles.FirstOrDefault(x => x.SourceRoleName == roleNameToLookup);
            role.Should().NotBeNull();
            role.IdentifierId.Should().NotBeNullOrWhiteSpace();
            role.RoleTypeId.Should().NotBeNullOrWhiteSpace();
            role.RoleTypeName.Should().Be(roleNameToLookup);
            role.SourceRoleName.Should().Be(roleNameToLookup);
            role.EffectiveFromDate.Should().Be(source.PARTY_ROLE_FROM_DATE);
            role.EffectiveToDate.Should().Be(source.PARTY_ROLE_TO_DATE);
        }
    }
}