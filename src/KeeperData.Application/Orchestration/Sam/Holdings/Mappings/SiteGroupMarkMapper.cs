using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;

namespace KeeperData.Application.Orchestration.Sam.Holdings.Mappings;

public static class SiteGroupMarkMapper
{
    public static List<SiteGroupMarkRelationshipDocument> ToGold(
        DateTime currentDateTime,
        List<SamHerdDocument>? silverHerds,
        string holdingIdentifier,
        string holdingIdentifierType)
    {
        if (silverHerds?.Count == 0)
            return [];

        // TODO - Add implementation

        return [];
    }

    public static SiteDocument? EnrichSiteWithGroupMarks(
        SiteDocument? goldSite,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks)
    {
        if (goldSite == null)
            return null;

        // TODO - Add implementation

        // TODO - Enrich with species
        // TODO - Enrich with marks
        // TODO - Enrich with activities

        return goldSite;
    }
}