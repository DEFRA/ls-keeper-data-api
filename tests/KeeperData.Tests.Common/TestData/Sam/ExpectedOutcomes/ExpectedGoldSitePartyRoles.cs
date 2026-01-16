namespace KeeperData.Tests.Common.TestData.Sam.ExpectedOutcomes;

public static class ExpectedGoldSitePartyRoles
{
    private static readonly string s_cphNumber = "12/345/6789";

    public static List<Core.Documents.SitePartyRoleRelationshipDocument> DefaultExpectedSitePartyRoles =>
    [
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("LIVESTOCKOWNER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKOWNER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("CPHHOLDER").id!,
            RoleTypeName = RoleData.Find("CPHHOLDER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("LIVESTOCKOWNER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKOWNER").name!,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("CPHHOLDER").id!,
            RoleTypeName = RoleData.Find("CPHHOLDER").name!,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000002",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000002",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("SHP").id!,
            SpeciesTypeCode = "SHP"
        }
    ];

    public static List<Core.Documents.SitePartyRoleRelationshipDocument> ExpectedSitePartyRoles_UpdatedHolderAndParties =>
    [
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000001",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("LIVESTOCKKEEPER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKKEEPER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000005",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("CPHHOLDER").id!,
            RoleTypeName = RoleData.Find("CPHHOLDER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            Id = Guid.NewGuid().ToString(),
            CustomerNumber = "C1000005",
            PartyTypeId = "Person",
            HoldingIdentifier = s_cphNumber,
            RoleTypeId = RoleData.Find("LIVESTOCKOWNER").id!,
            RoleTypeName = RoleData.Find("LIVESTOCKOWNER").name!,
            SpeciesTypeId = SpeciesData.Find("CTT").id!,
            SpeciesTypeCode = "CTT"
        }
    ];
}