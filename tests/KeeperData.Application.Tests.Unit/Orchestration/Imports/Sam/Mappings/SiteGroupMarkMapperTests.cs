using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SiteGroupMarkMapperTests
{
    /*
"2"|"I"|"333333"|"12/345/6789/01"|"CTT"|"CTT-BEEF-ADLR"|""|""|""|""|"C144743"|"C144743"|"2008-07-16 00:00:00"|""
"2"|"I"|"C100001"|"Mr"|"John"|""|""|"Doe"|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|"LIVESTOCKOWNER,LIVESTOCKKEEPER"|"2008-01-01 00:00:00"|""
"2"|"I"|"C100002"|"Mrs"|"Jane"|""|""|"Doe"|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|""|"LIVESTOCKKEEPER"|"2008-01-01 00:00:00"|""

PartyId							C100001							C100001                             C100002
PartyTypeId						Person							Person                              Person                
Herdmark						333333							333333							    333333	
CountyParishHoldingHerd			12/345/6789/01					12/345/6789/01						12/345/6789/01					
HoldingIdentifier				12/345/6789						12/345/6789                         12/345/6789
HoldingIdentifierType			CphNumber						CphNumber	                        CphNumber											
RoleTypeId						R100001							R100002		                        R100002						
RoleTypeCode					LIVESTOCKOWNER					LIVESTOCKKEEPER	                    LIVESTOCKKEEPER					
SpeciesTypeId					S100001							S100001		                        S100001							
SpeciesTypeCode					CTT								CTT		                            CTT						
ProductionUsageId				P100001							P100001	                            P100001									
ProductionUsageCode				BEEF							BEEF                                BEEF										
ProductionTypeId													                        						
ProductionTypeCode																				
DiseaseType																					
Interval																					
IntervalUnitOfTime																					
GroupMarkStartDate				2008-07-16						2008-07-16			                2008-07-16								
GroupMarkEndDate																 
    */

    private readonly List<SamHerd> _sourceSamHerds =
    [
        new()
        {
            BATCH_ID = 2,
            CHANGE_TYPE = "I",
            HERDMARK = "333333",
            CPHH = "12/345/6789/01",
            ANIMAL_SPECIES_CODE = "CTT",
            ANIMAL_PURPOSE_CODE = "CTT-BEEF-ADLR",
            OWNER_PARTY_IDS = "C100001",
            KEEPER_PARTY_IDS = "C100001,C100002",
            ANIMAL_GROUP_ID_MCH_FRM_DAT = new DateTime(2008, 7, 16, 0, 0, 0),
            ANIMAL_GROUP_ID_MCH_TO_DAT = null
        }
    ];

    private readonly List<SamParty> _sourceSamParties =
    [
        new()
        {
            BATCH_ID = 2,
            CHANGE_TYPE = "I",
            PARTY_ID = "C100001",
            PERSON_TITLE = "Mr",
            PERSON_GIVEN_NAME = "John",
            PERSON_FAMILY_NAME = "Doe",
            ROLES = "LIVESTOCKOWNER,LIVESTOCKKEEPER",
            COUNTRY_CODE = "GB"
        },
        new()
        {
            BATCH_ID = 2,
            CHANGE_TYPE = "I",
            PARTY_ID = "C100002",
            PERSON_TITLE = "Mrs",
            PERSON_GIVEN_NAME = "Jane",
            PERSON_FAMILY_NAME = "Doe",
            ROLES = "LIVESTOCKKEEPER",
            COUNTRY_CODE = "GB"
        }
    ];

    private readonly List<SiteGroupMarkRelationshipDocument> _expectedResult =
    [
        new()
        {
            Id = null,
            PartyId = "C100001",
            PartyTypeId = PartyType.Person.ToString(),
            Herdmark = "333333",
            CountyParishHoldingHerd = "12/345/6789/01",
            HoldingIdentifier = "12/345/6789",
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = "b2637b72-2196-4a19-bdf0-85c7ff66cf60",
            RoleTypeName = "Livestock Keeper",
            SpeciesTypeId = "5a86d64d-0f17-46a0-92d5-11fd5b2c5830",
            SpeciesTypeCode = "CTT",
            ProductionUsageId = "ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824",
            ProductionUsageCode = "BEEF",
            GroupMarkStartDate = new DateTime(2008, 7, 16, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = null,
            PartyId = "C100001",
            PartyTypeId = PartyType.Person.ToString(),
            Herdmark = "333333",
            CountyParishHoldingHerd = "12/345/6789/01",
            HoldingIdentifier = "12/345/6789",
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = "2de15dc1-19b9-4372-9e81-a9a2f87fd197",
            RoleTypeName = "Livestock Owner",
            SpeciesTypeId = "5a86d64d-0f17-46a0-92d5-11fd5b2c5830",
            SpeciesTypeCode = "CTT",
            ProductionUsageId = "ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824",
            ProductionUsageCode = "BEEF",
            GroupMarkStartDate = new DateTime(2008, 7, 16, 0, 0, 0),
            GroupMarkEndDate = null
        },
        new()
        {
            Id = null,
            PartyId = "C100002",
            PartyTypeId = PartyType.Person.ToString(),
            Herdmark = "333333",
            CountyParishHoldingHerd = "12/345/6789/01",
            HoldingIdentifier = "12/345/6789",
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = "b2637b72-2196-4a19-bdf0-85c7ff66cf60",
            RoleTypeName = "Livestock Keeper",
            SpeciesTypeId = "5a86d64d-0f17-46a0-92d5-11fd5b2c5830",
            SpeciesTypeCode = "CTT",
            ProductionUsageId = "ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824",
            ProductionUsageCode = "BEEF",
            GroupMarkStartDate = new DateTime(2008, 7, 16, 0, 0, 0),
            GroupMarkEndDate = null
        }
    ];

    private readonly SamHoldingImportSilverMappingStep _silverMappingStep;
    private readonly SamHoldingImportGoldMappingStep _goldMappingStep;

    private readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    private readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ICountryIdentifierLookupService> _countryIdentifierLookupServiceMock = new();

    public SiteGroupMarkMapperTests()
    {
        _productionUsageLookupServiceMock.Setup(x => x.FindAsync("BEEF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824", "Beef"));

        _speciesTypeLookupServiceMock.Setup(x => x.FindAsync("CTT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("5a86d64d-0f17-46a0-92d5-11fd5b2c5830", "Cattle"));

        _roleTypeLookupServiceMock.Setup(x => x.FindAsync("LIVESTOCKKEEPER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("b2637b72-2196-4a19-bdf0-85c7ff66cf60", "Livestock Keeper"));

        _roleTypeLookupServiceMock.Setup(x => x.FindAsync("LIVESTOCKOWNER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("2de15dc1-19b9-4372-9e81-a9a2f87fd197", "Livestock Owner"));

        _countryIdentifierLookupServiceMock.Setup(x => x.FindAsync("GB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("5e4b8d0d-96a8-4102-81e2-f067ee85d030", "United Kingdom"));

        _silverMappingStep = new SamHoldingImportSilverMappingStep(
            Mock.Of<IPremiseActivityTypeLookupService>(),
            Mock.Of<IPremiseTypeLookupService>(),
            _roleTypeLookupServiceMock.Object,
            _countryIdentifierLookupServiceMock.Object,
            _productionUsageLookupServiceMock.Object,
            _speciesTypeLookupServiceMock.Object,
            Mock.Of<ILogger<SamHoldingImportSilverMappingStep>>());

        _goldMappingStep = new SamHoldingImportGoldMappingStep(
            _countryIdentifierLookupServiceMock.Object,
            Mock.Of<IPremiseTypeLookupService>(),
            _speciesTypeLookupServiceMock.Object,
            _productionUsageLookupServiceMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportGoldMappingStep>>());
    }

    [Fact]
    public async Task GivenHerdsAndParties_WhenMappingToSilver_AndGold_ShouldCreateGroupMarks()
    {
        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            RawHoldings = [],
            RawHerds = _sourceSamHerds,
            RawParties = _sourceSamParties
        };

        await _silverMappingStep.ExecuteAsync(context, CancellationToken.None);

        VerifySilverData(context);

        await _goldMappingStep.ExecuteAsync(context, CancellationToken.None);

        VerifyGoldData(context);
    }

    private void VerifySilverData(SamHoldingImportContext context)
    {
        context.SilverHerds.Should().HaveCount(1);
        context.SilverHerds[0].SpeciesTypeId.Should().Be("5a86d64d-0f17-46a0-92d5-11fd5b2c5830");
        context.SilverHerds[0].SpeciesTypeCode.Should().Be("CTT");
        context.SilverHerds[0].ProductionUsageId.Should().Be("ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824");
        context.SilverHerds[0].ProductionUsageCode.Should().Be("BEEF");
        context.SilverHerds[0].AnimalPurposeCode.Should().Be("CTT-BEEF-ADLR");

        var silverParties = context.SilverParties.OrderBy(x => x.PartyId).ToList();
        silverParties.Should().HaveCount(2);
        silverParties[0].PartyId.Should().Be("C100001");
        silverParties[0].Roles!.Any(x => x.RoleTypeId == "2de15dc1-19b9-4372-9e81-a9a2f87fd197").Should().BeTrue();
        silverParties[0].Roles!.Any(x => x.RoleTypeName == "Livestock Owner").Should().BeTrue();
        silverParties[0].Roles!.Any(x => x.RoleTypeId == "b2637b72-2196-4a19-bdf0-85c7ff66cf60").Should().BeTrue();
        silverParties[0].Roles!.Any(x => x.RoleTypeName == "Livestock Keeper").Should().BeTrue();

        silverParties[1].PartyId.Should().Be("C100002");
        silverParties[1].Roles!.Any(x => x.RoleTypeId == "b2637b72-2196-4a19-bdf0-85c7ff66cf60").Should().BeTrue();
        silverParties[1].Roles!.Any(x => x.RoleTypeName == "Livestock Keeper").Should().BeTrue();

        var silverPartyRoles = context.SilverPartyRoles.OrderBy(x => x.PartyId).ThenBy(x => x.RoleTypeName).ToList();
        silverPartyRoles.Should().HaveCount(3);
        silverPartyRoles[0].PartyId.Should().Be("C100001");
        silverPartyRoles[0].RoleTypeId.Should().Be("b2637b72-2196-4a19-bdf0-85c7ff66cf60");
        silverPartyRoles[0].RoleTypeName.Should().Be("Livestock Keeper");
        silverPartyRoles[0].HoldingIdentifier.Should().Be("12/345/6789");

        silverPartyRoles[1].PartyId.Should().Be("C100001");
        silverPartyRoles[1].RoleTypeId.Should().Be("2de15dc1-19b9-4372-9e81-a9a2f87fd197");
        silverPartyRoles[1].RoleTypeName.Should().Be("Livestock Owner");
        silverPartyRoles[1].HoldingIdentifier.Should().Be("12/345/6789");

        silverPartyRoles[2].PartyId.Should().Be("C100002");
        silverPartyRoles[2].RoleTypeId.Should().Be("b2637b72-2196-4a19-bdf0-85c7ff66cf60");
        silverPartyRoles[2].RoleTypeName.Should().Be("Livestock Keeper");
        silverPartyRoles[2].HoldingIdentifier.Should().Be("12/345/6789");
    }

    private void VerifyGoldData(SamHoldingImportContext context)
    {
        context.GoldSiteGroupMarks.Should().HaveCount(3);

        context.GoldSiteGroupMarks.OrderBy(x => x.PartyId).ThenBy(x => x.RoleTypeName).ToList()
            .Should().BeEquivalentTo(_expectedResult, options => options.Excluding(x => x.LastUpdatedDate));
    }
}