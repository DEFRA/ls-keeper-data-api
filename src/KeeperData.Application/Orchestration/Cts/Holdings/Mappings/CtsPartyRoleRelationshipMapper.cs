using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Application.Orchestration.Cts.Holdings.Mappings;

public static class CtsPartyRoleRelationshipMapper
{
    public static List<PartyRoleRelationshipDocument> ToSilver(
        List<CtsPartyDocument> silverParties,
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
                Source = SourceSystemType.CTS.ToString(),

                RoleTypeId = role.RoleTypeId,
                RoleTypeName = role.RoleTypeName,
                SourceRoleName = role.SourceRoleName,

                EffectiveFromData = role.EffectiveFromDate,
                EffectiveToData = role.EffectiveToDate,

                LastUpdatedBatchId = party.LastUpdatedBatchId
            });

        return result?.ToList() ?? [];
    }
}