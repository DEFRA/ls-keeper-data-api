using FluentAssertions;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Domain.Sites.Formatters;
using KeeperData.Core.Extensions;

namespace KeeperData.Tests.Common.Mappings;

public static class VerifyCtsAgentOrKeeperMappings
{
    public static void VerifyMapping_From_CtsAgentOrKeeper_To_CtsPartyDocument(CtsAgentOrKeeper source, CtsPartyDocument target, InferredRoleType inferredRoleType)
    {
        source.Should().NotBeNull();
        target.Should().NotBeNull();

        target.Id.Should().BeNull();
        target.LastUpdatedBatchId.Should().Be(source.BATCH_ID);
        target.Deleted.Should().BeFalse();

        target.CountyParishHoldingNumber.Should().Be(source.LID_FULL_IDENTIFIER.LidIdentifierToCph());

        target.PartyId.Should().Be(source.PAR_ID);

        var expectedPartyTypeId = !string.IsNullOrWhiteSpace(source.PAR_SURNAME)
            && !string.IsNullOrWhiteSpace(source.PAR_TITLE) ? PartyType.Person.ToString()
            : PartyType.Business.ToString();
        target.PartyTypeId.Should().Be(expectedPartyTypeId);

        target.PartyFullName.Should().BeNull();

        target.PartyTitleTypeIdentifier.Should().Be(source.PAR_TITLE);
        target.PartyFirstName.Should().Be(source.PAR_INITIALS);
        target.PartyLastName.Should().Be(source.PAR_SURNAME);

        // Address
        var address = target.Address;
        address.Should().NotBeNull();
        address.IdentifierId.Should().NotBeNullOrWhiteSpace();
        address.AddressLine.Should().Be(source.ADR_ADDRESS_2);
        address.AddressLocality.Should().Be(source.ADR_ADDRESS_3);
        address.AddressStreet.Should().Be(source.ADR_ADDRESS_4);
        address.AddressTown.Should().Be(source.ADR_ADDRESS_5);
        address.AddressPostCode.Should().Be(source.ADR_POST_CODE);
        address.CountryIdentifier.Should().BeNull();
        address.CountryCode.Should().BeNull();
        address.UniquePropertyReferenceNumber.Should().BeNull();

        // Communication
        var comms = target.Communication;
        comms.Should().NotBeNull();
        comms.IdentifierId.Should().NotBeNullOrWhiteSpace();
        comms.Email.Should().Be(source.PAR_EMAIL_ADDRESS);
        comms.Mobile.Should().Be(source.PAR_MOBILE_NUMBER);
        comms.Landline.Should().Be(source.PAR_TEL_NUMBER);

        // Roles
        target.Roles.Should().NotBeNull().And.HaveCount(1);

        var roleToLookup = EnumExtensions.GetDescription(inferredRoleType);
        var expectedRoleTypeName = inferredRoleType == InferredRoleType.Agent
            ? "Agent"
            : "LivestockKeeper";

        var role = target.Roles[0];
        role.Should().NotBeNull();
        role.IdentifierId.Should().NotBeNullOrWhiteSpace();
        role.RoleTypeId.Should().NotBeNullOrWhiteSpace();
        role.RoleTypeCode.Should().Be(roleToLookup);
        role.RoleTypeName.Should().Be(expectedRoleTypeName);
        role.SourceRoleName.Should().Be(roleToLookup);
        role.EffectiveFromDate.Should().Be(source.LPR_EFFECTIVE_FROM_DATE);
        role.EffectiveToDate.Should().Be(source.LPR_EFFECTIVE_TO_DATE);
    }
}