using FluentAssertions;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Tests.Common.Mappings;

public static class VerifyCtsPartyRoleRelationshipMappings
{
    public static void VerifyMapping_From_CtsPartyDocument_To_PartyRoleRelationshipDocument(
        CtsPartyDocument source,
        Core.Documents.Silver.SitePartyRoleRelationshipDocument target,
        string expectedHoldingIdentifier)
    {
        source.Should().NotBeNull();
        target.Should().NotBeNull();

        source.Roles.Should().NotBeNullOrEmpty();

        var matchingRole = source.Roles.SingleOrDefault(r => r.RoleTypeId == target.RoleTypeId);
        matchingRole.Should().NotBeNull($"Expected role with ID {target.RoleTypeId} to exist in source.Roles");

        target.Id.Should().NotBeNullOrWhiteSpace();

        target.PartyId.Should().Be(source.PartyId);
        target.PartyTypeId.Should().Be(source.PartyTypeId);
        target.HoldingIdentifier.Should().Be(expectedHoldingIdentifier);
        target.Source.Should().Be(SourceSystemType.CTS.ToString());

        target.RoleTypeId.Should().Be(matchingRole.RoleTypeId);
        target.RoleTypeCode.Should().Be(matchingRole.RoleTypeCode);
        target.RoleTypeName.Should().Be(matchingRole.RoleTypeName);
        target.SourceRoleName.Should().Be(matchingRole.SourceRoleName);

        target.EffectiveFromData.Should().Be(matchingRole.EffectiveFromDate);
        target.EffectiveToData.Should().Be(matchingRole.EffectiveToDate);

        target.LastUpdatedBatchId.Should().Be(source.LastUpdatedBatchId);
    }
}