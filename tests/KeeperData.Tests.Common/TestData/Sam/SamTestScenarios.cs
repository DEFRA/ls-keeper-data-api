using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;

namespace KeeperData.Tests.Common.TestData.Sam;

public static class SamTestScenarios
{
    public static string CphNumber = "12/345/6789";

    public static SamTestScenarioData DefaultScenario => new SamTestScenarioData
    {
        Cph = CphNumber,
        RawHoldings = [],
        RawHerds = [
            new()
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                HERDMARK = "123456",
                CPHH = $"{CphNumber}/01",
                ANIMAL_SPECIES_CODE = "CTT",
                ANIMAL_PURPOSE_CODE = "CTT-BEEF-ADLR",
                OWNER_PARTY_IDS = "C100001",
                KEEPER_PARTY_IDS = "C100001,C100002",
                ANIMAL_GROUP_ID_MCH_FRM_DAT = new DateTime(2001, 1, 1, 0, 0, 0),
                ANIMAL_GROUP_ID_MCH_TO_DAT = null
            }
        ],
        RawHolders = [],
        RawParties = [
            new()
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                PARTY_ID = "C100001",
                PERSON_TITLE = "Mr",
                PERSON_GIVEN_NAME = "John",
                PERSON_FAMILY_NAME = "Doe",
                ROLES = "LIVESTOCKOWNER,LIVESTOCKKEEPER",
                COUNTRY_CODE = "GB",
                PARTY_ROLE_FROM_DATE = new DateTime(2001, 1, 1, 0, 0, 0),
                PARTY_ROLE_TO_DATE = null,
                IsDeleted = false
            },
            new()
            {
                BATCH_ID = 1,
                CHANGE_TYPE = "I",
                PARTY_ID = "C100002",
                PERSON_TITLE = "Mrs",
                PERSON_GIVEN_NAME = "Jane",
                PERSON_FAMILY_NAME = "Doe",
                ROLES = "LIVESTOCKKEEPER",
                COUNTRY_CODE = "GB",
                PARTY_ROLE_FROM_DATE = new DateTime(2001, 1, 1, 0, 0, 0),
                PARTY_ROLE_TO_DATE = null,
                IsDeleted = false
            }
        ],
        ExpectedGoldSite = new SiteDocument
        {
            Id = Guid.NewGuid().ToString()
        },
        ExpectedGoldParties = [],
        ExpectedGoldSitePartyRoles = [],
        ExpectedGoldSiteGroupMarks = []
    };

    public class SamTestScenarioData
    {
        public required string Cph { get; init; }

        public List<SamCphHolding> RawHoldings { get; set; } = [];
        public List<SamHerd> RawHerds { get; set; } = [];
        public List<SamCphHolder> RawHolders { get; set; } = [];
        public List<SamParty> RawParties { get; set; } = [];

        public SiteDocument? ExpectedGoldSite { get; set; }
        public List<PartyDocument> ExpectedGoldParties { get; set; } = [];
        public List<Core.Documents.SitePartyRoleRelationshipDocument> ExpectedGoldSitePartyRoles { get; set; } = [];
        public List<SiteGroupMarkRelationshipDocument> ExpectedGoldSiteGroupMarks { get; set; } = [];
    }
}
