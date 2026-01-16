using KeeperData.Core.Documents;
using System.Data;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SitePartyRoleMapper
{
    public static List<Core.Documents.SitePartyRoleRelationshipDocument> ToGold(
        List<PartyDocument> goldParties,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks,
        string goldSiteId,
        string holdingIdentifier)
    {
        if (goldParties == null) return [];

        return [.. goldParties
            .Where(party => party.Deleted != true && party.PartyRoles != null)
            .SelectMany(party =>
                party.PartyRoles!
                    .Where(role => role.Role.IdentifierId != null &&
                                   role.Site != null &&
                                   role.Site.IdentifierId == goldSiteId)
                    .SelectMany(role =>
                    {
                        var matchingSpecies = goldSiteGroupMarks
                            .Where(g =>
                                g.HoldingIdentifier == holdingIdentifier &&
                                g.CustomerNumber == party.CustomerNumber &&
                                g.RoleTypeId == role.Role.IdentifierId)
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

                            CustomerNumber = party.CustomerNumber ?? string.Empty,
                            PartyTypeId = party.PartyType ?? string.Empty,
                            HoldingIdentifier = holdingIdentifier,

                            RoleTypeId = role.Role.IdentifierId!,
                            RoleTypeName = role.Role.Name,

                            SpeciesTypeId = species.SpeciesTypeId ?? string.Empty,
                            SpeciesTypeCode = species.SpeciesTypeCode ?? string.Empty
                        });
                    }))];
    }
}