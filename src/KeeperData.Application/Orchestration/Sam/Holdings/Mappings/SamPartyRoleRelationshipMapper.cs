using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SamPartyRoleRelationshipMapper
{
    public static List<PartyRoleRelationshipDocument> ToSilver(
        List<SamPartyDocument> silverParties,
        string holdingIdentifier,
        string holdingIdentifierType)
    {
        var result = silverParties?
            .Where(x => x.Roles != null)
            .SelectMany(x => x.Roles!, (party, role) => new PartyRoleRelationshipDocument
            {
                Id = role.IdentifierId,
                PartyId = party.PartyId,
                PartyTypeId = party.PartyTypeId,
                HoldingIdentifier = holdingIdentifier,
                HoldingIdentifierType = holdingIdentifierType,
                Source = SourceSystemType.SAM.ToString(),

                RoleTypeId = role.RoleTypeId,
                RoleTypeName = role.RoleTypeName,
                SourceRoleName = role.SourceRoleName,

                EffectiveFromData = role.EffectiveFromData,
                EffectiveToData = role.EffectiveToData,

                LastUpdatedBatchId = party.LastUpdatedBatchId
            });

        return result?.ToList() ?? [];
    }
}