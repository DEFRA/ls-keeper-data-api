using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SiteGroupMarkMapper
{
    public static List<SiteGroupMarkRelationshipDocument> ToGold(
        DateTime currentDateTime,
        List<SamHerdDocument>? silverHerds,
        List<Core.Documents.Silver.SitePartyRoleRelationshipDocument> silverSitePartyRoles,
        string holdingIdentifier,
        string holdingIdentifierType)
    {
        if (silverHerds is null || silverHerds.Count == 0)
            return [];

        var result = new List<SiteGroupMarkRelationshipDocument>();

        foreach (var herd in silverHerds?.Where(x => x.Deleted != true) ?? Enumerable.Empty<SamHerdDocument>())
        {
            var baseDoc = new SiteGroupMarkRelationshipDocument
            {
                // Id - Leave to support upsert assigning Id

                LastUpdatedDate = currentDateTime,
                Herdmark = herd.Herdmark,
                CountyParishHoldingHerd = herd.CountyParishHoldingHerd,
                HoldingIdentifier = holdingIdentifier,
                HoldingIdentifierType = holdingIdentifierType,
                SpeciesTypeId = herd.SpeciesTypeId,
                SpeciesTypeCode = herd.SpeciesTypeCode,
                ProductionUsageId = herd.ProductionUsageId,
                ProductionUsageCode = herd.ProductionUsageCode,
                ProductionTypeId = herd.ProductionTypeId,
                ProductionTypeCode = herd.ProductionTypeCode,
                DiseaseType = herd.DiseaseType,
                Interval = herd.Interval,
                IntervalUnitOfTime = herd.IntervalUnitOfTime,
                GroupMarkStartDate = herd.GroupMarkStartDate,
                GroupMarkEndDate = herd.GroupMarkEndDate
            };

            var allPartyIds = herd.OwnerPartyIdList
                .Concat(herd.KeeperPartyIdList).Distinct();

            foreach (var partyId in allPartyIds)
            {
                var matchingRoles = silverSitePartyRoles
                    .Where(r => r.PartyId == partyId
                        && r.HoldingIdentifier == holdingIdentifier
                        && r.HoldingIdentifierType == holdingIdentifierType);

                foreach (var role in matchingRoles)
                {
                    var doc = baseDoc with
                    {
                        PartyId = role.PartyId,
                        PartyTypeId = role.PartyTypeId,
                        RoleTypeId = role.RoleTypeId,
                        RoleTypeName = role.RoleTypeName
                    };

                    result.Add(doc);
                }
            }
        }

        return result;
    }
}