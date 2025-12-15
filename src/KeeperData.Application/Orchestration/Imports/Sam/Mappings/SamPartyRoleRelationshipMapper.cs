using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SamPartyRoleRelationshipMapper
{
    public static List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> ToSilver(
        List<SamPartyDocument> silverParties,
        string holdingIdentifier)
    {
        if (silverParties == null) return [];

        return [.. silverParties
            .Where(party => party.Deleted != true && party.Roles != null)
            .SelectMany(party =>
            {
                var holdingIdentifiers = new List<string>() { holdingIdentifier };

                return holdingIdentifiers.SelectMany(cph => party.Roles!.Where(x => x.RoleTypeId != null)
                    .Select(role => new Core.Documents.Silver.SitePartyRoleRelationshipDocument
                    {
                        // Id - Leave to support upsert assigning Id

                        PartyId = party.PartyId,
                        PartyTypeId = party.PartyTypeId,
                        HoldingIdentifier = cph,
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

    public static List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> ToSilverUsingCphList(
        List<SamPartyDocument> silverParties,
        string? holdingIdentifier = null)
    {
        if (silverParties == null) return [];

        return [.. silverParties
            .Where(party => party.Deleted != true && party.Roles != null)
            .SelectMany(party =>
            {
                var holdingIdentifiers = party.CphList?.Count > 0
                    ? party.CphList
                    : holdingIdentifier is not null ? [holdingIdentifier] : [];

                return holdingIdentifiers.SelectMany(cph => party.Roles!.Where(x => x.RoleTypeId != null)
                    .Select(role => new Core.Documents.Silver.SitePartyRoleRelationshipDocument
                    {
                        // Id - Leave to support upsert assigning Id

                        PartyId = party.PartyId,
                        PartyTypeId = party.PartyTypeId,
                        HoldingIdentifier = cph,
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