using KeeperData.Core.Documents;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SitePartyRoleMapper
{
    public static List<Core.Documents.SitePartyRoleRelationshipDocument> ToGold(
        List<PartyDocument> goldParties,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        string holdingIdentifierType,
        string holdingIdentifier)
    {
        if (goldParties == null) return [];

        return [.. goldParties
            .Where(party => party.Deleted != true && party.PartyRoles != null)
            .SelectMany(party =>
            {
                var holdingIdentifiers = new List<string>() { holdingIdentifier };

                return holdingIdentifiers.SelectMany(cph =>
                    party.PartyRoles!.Where(x => x.Role.IdentifierId != null)
                    .SelectMany(role =>
                    {
                        var matchingSpecies = goldSiteGroupMarks
                            .Where(g =>
                                g.PartyId == party.CustomerNumber &&
                                g.RoleTypeId == role.Role.IdentifierId &&
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

                            PartyId = party.CustomerNumber ?? string.Empty,
                            PartyTypeId = party.PartyType ?? string.Empty,
                            HoldingIdentifier = cph,
                            HoldingIdentifierType = holdingIdentifierType,

                            RoleTypeId = role.Role.IdentifierId!,
                            RoleTypeName = role.Role.Name,

                            SpeciesTypeId = species.SpeciesTypeId ?? "",
                            SpeciesTypeCode = species.SpeciesTypeCode ?? ""
                        });
                    }));
            })];
    }
}