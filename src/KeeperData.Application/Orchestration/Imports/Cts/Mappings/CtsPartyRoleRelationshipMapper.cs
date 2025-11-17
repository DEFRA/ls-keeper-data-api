using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Application.Orchestration.Imports.Cts.Mappings;

public static class CtsPartyRoleRelationshipMapper
{
    public static List<SitePartyRoleRelationshipDocument> ToSilver(List<CtsPartyDocument> silverParties)
    {
        var result = silverParties?
            .Where(x => x.Roles != null)
            .SelectMany(x => x.Roles!, (party, role) => new SitePartyRoleRelationshipDocument
            {
                // Id - Leave to support upsert assigning Id

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