using FluentAssertions;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Tests.Common.Mappings;

public static class VerifySamPartyRoleRelationshipMappings
{
    public static void VerifyMapping_From_SamPartyDocument_To_PartyRoleRelationshipDocument(
        SamPartyDocument source,
        PartyRoleRelationshipDocument target,
        string expectedHoldingIdentifier,
        string expectedHoldingIdentifierType)
    {
        source.Should().NotBeNull();
        target.Should().NotBeNull();

        source.Roles.Should().NotBeNullOrEmpty();

        var matchingRole = source.Roles.SingleOrDefault(r => r.IdentifierId == target.Id);
        matchingRole.Should().NotBeNull($"Expected role with ID {target.Id} to exist in source.Roles");

        target.Id.Should().Be(matchingRole.IdentifierId);
        target.PartyId.Should().Be(source.PartyId);
        target.PartyTypeId.Should().Be(source.PartyTypeId);
        target.HoldingIdentifier.Should().Be(expectedHoldingIdentifier);
        target.HoldingIdentifierType.Should().Be(expectedHoldingIdentifierType);
        target.Source.Should().Be(SourceSystemType.SAM.ToString());

        target.RoleTypeId.Should().Be(matchingRole.RoleTypeId);
        target.RoleTypeName.Should().Be(matchingRole.RoleTypeName);
        target.SourceRoleName.Should().Be(matchingRole.SourceRoleName);

        target.EffectiveFromData.Should().Be(matchingRole.EffectiveFromData);
        target.EffectiveToData.Should().Be(matchingRole.EffectiveToData);

        target.LastUpdatedBatchId.Should().Be(source.LastUpdatedBatchId);
    }
}