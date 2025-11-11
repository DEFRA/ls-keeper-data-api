using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Application.Orchestration.Cts.Holdings.Mappings;

public static class CtsPartyRoleRelationshipMapper
{
    public static List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> ToSilver(List<CtsPartyDocument> silverParties)
    {
        var result = silverParties?
            .Where(x => x.Roles != null)
            .SelectMany(x => x.Roles!, (party, role) => new Core.Documents.Silver.SitePartyRoleRelationshipDocument
            {
                Id = role.IdentifierId,
                PartyId = party.PartyId,
                PartyTypeId = party.PartyTypeId,
                HoldingIdentifier = party.CountyParishHoldingNumber,
                HoldingIdentifierType = party.HoldingIdentifierType,
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