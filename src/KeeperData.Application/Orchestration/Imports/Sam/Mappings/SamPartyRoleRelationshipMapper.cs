using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamPartyRoleRelationshipMapper
{
    public static List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> ToSilver(
        List<SamPartyDocument> silverParties,
        string holdingIdentifierType,
        string? holdingIdentifier = null)
    {
        if (silverParties == null) return [];

        return [.. silverParties
            .Where(party => party.Roles != null)
            .SelectMany(party =>
            {
                var cphs = party.IsHolder ? party.CphList : null;
                var holdingIdentifiers = cphs?.Count > 0
                    ? cphs
                    : holdingIdentifier is not null ? [holdingIdentifier] : [];

                return holdingIdentifiers.SelectMany(cph => party.Roles!.Select(role =>
                    new Core.Documents.Silver.SitePartyRoleRelationshipDocument
                    {
                        // Id - Leave to support upsert assigning Id

                        PartyId = party.PartyId,
                        PartyTypeId = party.PartyTypeId,
                        IsHolder = party.IsHolder,
                        HoldingIdentifier = cph,
                        HoldingIdentifierType = holdingIdentifierType,
                        Source = SourceSystemType.SAM.ToString(),

                        RoleTypeId = role.RoleTypeId,
                        RoleTypeName = role.RoleTypeName,
                        SourceRoleName = role.SourceRoleName,

                        EffectiveFromData = role.EffectiveFromDate,
                        EffectiveToData = role.EffectiveToDate,

                        LastUpdatedBatchId = party.LastUpdatedBatchId
                    }));
            })];
    }
}