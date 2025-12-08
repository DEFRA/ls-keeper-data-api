using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Documents;
using KeeperData.Core.Domain.Enums;
using KeeperData.Core.Repositories;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Mappings;

public class SitePartyRoleMapperTests
{
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
            COUNTRY_CODE = "GB",
            PARTY_ROLE_FROM_DATE = new DateTime(2010, 1, 1, 0, 0, 0),
            PARTY_ROLE_TO_DATE = null,
            IsDeleted = false
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
            COUNTRY_CODE = "GB",
            PARTY_ROLE_FROM_DATE = new DateTime(2011, 1, 1, 0, 0, 0),
            PARTY_ROLE_TO_DATE = null,
            IsDeleted = false
        },
        new()
        {
            BATCH_ID = 2,
            CHANGE_TYPE = "I",
            PARTY_ID = "C100003",
            PERSON_TITLE = "Mr",
            PERSON_GIVEN_NAME = "John",
            PERSON_FAMILY_NAME = "Smith",
            ROLES = "LIVESTOCKKEEPER",
            COUNTRY_CODE = "GB",
            PARTY_ROLE_FROM_DATE = new DateTime(2012, 1, 1, 0, 0, 0),
            PARTY_ROLE_TO_DATE = null,
            IsDeleted = true
        }
    ];

    private readonly List<Core.Documents.SitePartyRoleRelationshipDocument> _expectedResult =
    [
        new()
        {
            PartyId = "C100001",
            PartyTypeId = PartyType.Person.ToString(),
            HoldingIdentifier = "12/345/6789",
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = "b2637b72-2196-4a19-bdf0-85c7ff66cf60",
            RoleTypeName = "Livestock Keeper",
            EffectiveFromData = new DateTime(2010, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = "5a86d64d-0f17-46a0-92d5-11fd5b2c5830",
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            PartyId = "C100001",
            PartyTypeId = PartyType.Person.ToString(),
            HoldingIdentifier = "12/345/6789",
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = "2de15dc1-19b9-4372-9e81-a9a2f87fd197",
            RoleTypeName = "Livestock Owner",
            EffectiveFromData = new DateTime(2010, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = "5a86d64d-0f17-46a0-92d5-11fd5b2c5830",
            SpeciesTypeCode = "CTT"
        },
        new()
        {
            PartyId = "C100002",
            PartyTypeId = PartyType.Person.ToString(),
            HoldingIdentifier = "12/345/6789",
            HoldingIdentifierType = HoldingIdentifierType.CphNumber.ToString(),
            RoleTypeId = "b2637b72-2196-4a19-bdf0-85c7ff66cf60",
            RoleTypeName = "Livestock Keeper",
            EffectiveFromData = new DateTime(2011, 1, 1, 0, 0, 0),
            EffectiveToData = null,
            SpeciesTypeId = "5a86d64d-0f17-46a0-92d5-11fd5b2c5830",
            SpeciesTypeCode = "CTT"
        }
    ];

    private readonly SamHoldingImportSilverMappingStep _silverMappingStep;
    private readonly SamHoldingImportGoldMappingStep _goldMappingStep;

    private readonly Mock<IProductionUsageLookupService> _productionUsageLookupServiceMock = new();
    private readonly Mock<IPremiseActivityTypeLookupService> _premiseActivityTypeLookupServiceMock = new();
    private readonly Mock<ISpeciesTypeLookupService> _speciesTypeLookupServiceMock = new();
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();

    private readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();

    public SitePartyRoleMapperTests()
    {
        _productionUsageLookupServiceMock.Setup(x => x.FindAsync("BEEF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824", "Beef"));

        _premiseActivityTypeLookupServiceMock.Setup(x => x.FindAsync("RM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("d2d9be5e-18b4-4424-b196-fd40f3b105d8", "Red Meat"));

        _speciesTypeLookupServiceMock.Setup(x => x.FindAsync("CTT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("5a86d64d-0f17-46a0-92d5-11fd5b2c5830", "Cattle"));

        _roleTypeLookupServiceMock.Setup(x => x.FindAsync("LIVESTOCKKEEPER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("b2637b72-2196-4a19-bdf0-85c7ff66cf60", "Livestock Keeper"));

        _roleTypeLookupServiceMock.Setup(x => x.FindAsync("LIVESTOCKOWNER", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("2de15dc1-19b9-4372-9e81-a9a2f87fd197", "Livestock Owner"));

        _goldSiteRepositoryMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteDocument?)null);

        _silverMappingStep = new SamHoldingImportSilverMappingStep(
            Mock.Of<IPremiseActivityTypeLookupService>(),
            Mock.Of<IPremiseTypeLookupService>(),
            _roleTypeLookupServiceMock.Object,
            Mock.Of<ICountryIdentifierLookupService>(),
            _productionUsageLookupServiceMock.Object,
            _speciesTypeLookupServiceMock.Object,
            Mock.Of<ILogger<SamHoldingImportSilverMappingStep>>());

        _goldMappingStep = new SamHoldingImportGoldMappingStep(
            Mock.Of<ICountryIdentifierLookupService>(),
            Mock.Of<IPremiseTypeLookupService>(),
            _speciesTypeLookupServiceMock.Object,
            _premiseActivityTypeLookupServiceMock.Object,
            _goldSiteRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportGoldMappingStep>>());
    }

    [Fact]
    public async Task GivenHerdsAndParties_WhenMappingToSilver_AndGold_ShouldCreateSitePartyRoles()
    {
        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            RawHoldings = GenerateSamCphHolding("12/345/6789", 1),
            RawHerds = _sourceSamHerds,
            RawParties = _sourceSamParties
        };

        await _silverMappingStep.ExecuteAsync(context, CancellationToken.None);

        VerifySilverData(context);

        await _goldMappingStep.ExecuteAsync(context, CancellationToken.None);

        VerifyGoldData(context);
    }

    private static void VerifySilverData(SamHoldingImportContext context)
    {
        context.SilverHerds.Should().HaveCount(1);
        context.SilverHerds[0].SpeciesTypeId.Should().Be("5a86d64d-0f17-46a0-92d5-11fd5b2c5830");
        context.SilverHerds[0].SpeciesTypeCode.Should().Be("CTT");
        context.SilverHerds[0].ProductionUsageId.Should().Be("ba9cb8fb-ab7f-42f2-bc1f-fa4d7fda4824");
        context.SilverHerds[0].ProductionUsageCode.Should().Be("BEEF");
        context.SilverHerds[0].AnimalPurposeCode.Should().Be("CTT-BEEF-ADLR");

        var silverParties = context.SilverParties.OrderBy(x => x.PartyId).ToList();
        silverParties.Should().HaveCount(3);
        silverParties[0].PartyId.Should().Be("C100001");
        silverParties[0].Roles!.Any(x => x.RoleTypeId == "2de15dc1-19b9-4372-9e81-a9a2f87fd197").Should().BeTrue();
        silverParties[0].Roles!.Any(x => x.RoleTypeName == "Livestock Owner").Should().BeTrue();
        silverParties[0].Roles!.Any(x => x.RoleTypeId == "b2637b72-2196-4a19-bdf0-85c7ff66cf60").Should().BeTrue();
        silverParties[0].Roles!.Any(x => x.RoleTypeName == "Livestock Keeper").Should().BeTrue();
        silverParties[0].Deleted.Should().BeFalse();

        silverParties[1].PartyId.Should().Be("C100002");
        silverParties[1].Roles!.Any(x => x.RoleTypeId == "b2637b72-2196-4a19-bdf0-85c7ff66cf60").Should().BeTrue();
        silverParties[1].Roles!.Any(x => x.RoleTypeName == "Livestock Keeper").Should().BeTrue();
        silverParties[1].Deleted.Should().BeFalse();

        silverParties[2].PartyId.Should().Be("C100003");
        silverParties[2].Roles!.Any(x => x.RoleTypeId == "b2637b72-2196-4a19-bdf0-85c7ff66cf60").Should().BeTrue();
        silverParties[2].Roles!.Any(x => x.RoleTypeName == "Livestock Keeper").Should().BeTrue();
        silverParties[2].Deleted.Should().BeTrue();

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
        context.GoldSitePartyRoles.Should().HaveCount(3);

        context.GoldSitePartyRoles.OrderBy(x => x.PartyId).ThenBy(x => x.RoleTypeName).ToList()
            .Should().BeEquivalentTo(_expectedResult);
    }

    private static List<SamCphHolding> GenerateSamCphHolding(string holdingIdentifier, int quantity)
    {
        var records = new List<SamCphHolding>();
        var factory = new MockSamRawDataFactory();
        for (var i = 0; i < quantity; i++)
        {
            records.Add(factory.CreateMockHolding(
                changeType: DataBridgeConstants.ChangeTypeInsert,
                batchId: 1,
                holdingIdentifier: holdingIdentifier,
                endDate: DateTime.UtcNow.Date));
        }
        return records;
    }
}