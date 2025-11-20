using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SitePartyRoleMapper
{
    public static List<Core.Documents.SitePartyRoleRelationshipDocument> ToGold(
        DateTime currentDateTime,
        List<SamPartyDocument> silverParties,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        string holdingIdentifierType,
        string? holdingIdentifier = null)
    {
        if (silverParties == null) return [];

        return [.. silverParties
            .Where(party => party.Roles != null)
            .SelectMany(party =>
            {
                var holdingIdentifiers = party.CphList?.Count > 0
                    ? party.CphList
                    : holdingIdentifier is not null ? [holdingIdentifier] : [];

                return holdingIdentifiers.SelectMany(cph =>
                    party.Roles!.Where(x => x.RoleTypeId != null)
                    .SelectMany(role =>
                    {
                        var matchingSpecies = goldSiteGroupMarks
                            .Where(g =>
                                g.PartyId == party.PartyId &&
                                g.RoleTypeId == role.RoleTypeId &&
                                g.HoldingIdentifier == cph &&
                                g.HoldingIdentifierType == holdingIdentifierType)
                            .Select(g => new
                            {
                                g.SpeciesTypeId,
                                g.SpeciesTypeCode
                            })
                            .Distinct()
                            .DefaultIfEmpty(new { SpeciesTypeId = (string?)null, SpeciesTypeCode = (string?)null });

                        return matchingSpecies.Select(species => new Core.Documents.SitePartyRoleRelationshipDocument
                        {
                            // Id left unset for upsert

                            PartyId = party.PartyId,
                            PartyTypeId = party.PartyTypeId,
                            HoldingIdentifier = cph,
                            HoldingIdentifierType = holdingIdentifierType,

                            RoleTypeId = role.RoleTypeId!,
                            RoleTypeName = role.RoleTypeName,

                            EffectiveFromData = role.EffectiveFromDate,
                            EffectiveToData = role.EffectiveToDate,

                            SpeciesTypeId = species.SpeciesTypeId ?? "",
                            SpeciesTypeCode = species.SpeciesTypeCode ?? ""
                        });
                    }));
            })];
    }
}