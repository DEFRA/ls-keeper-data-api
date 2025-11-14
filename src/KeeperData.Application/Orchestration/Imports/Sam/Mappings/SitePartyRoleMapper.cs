using KeeperData.Core.Documents;

namespace KeeperData.Application.Orchestration.Imports.Sam.Mappings;

public static class SitePartyRoleMapper
{
    public static List<Core.Documents.SitePartyRoleRelationshipDocument> ToGold(
        DateTime currentDateTime,
        List<PartyDocument> goldParties,
        List<SiteGroupMarkRelationshipDocument> goldSiteGroupMarks)
    {
        // TODO - Add implementation

        return [];
    }

    public static SiteDocument? EnrichSiteWithParties(
        SiteDocument? goldSite,
        List<PartyDocument> goldParties)
    {
        if (goldSite == null)
            return null;

        // TODO - Add implementation

        return goldSite;
    }
}