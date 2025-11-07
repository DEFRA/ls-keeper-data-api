using AutoFixture;
using KeeperData.Application.Orchestration.Sam.Holdings;
using KeeperData.Application.Orchestration.Sam.Holdings.Steps;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Sam.Holdings.Steps;

public class SamHoldingImportPersistenceStepTests
{
    private readonly Fixture _fixture;

    private readonly Mock<IGenericRepository<SamHoldingDocument>> _silverHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<ISilverSitePartyRoleRelationshipRepository> _silverSitePartyRoleRelationshipRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamHerdDocument>> _silverHerdRepositoryMock = new();

    private readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();
    private readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>> _goldSitePartyRoleRelationshipRepositoryMock = new();
    private readonly Mock<IGenericRepository<SiteGroupMarkRelationshipDocument>> _goldSiteGroupMarkRelationshipRepositoryMock = new();

    public SamHoldingImportPersistenceStepTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenIncomingHoldingsEmpty_WhenStepExecuted_ShouldDeleteOrphans()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverHoldings = []
        };

        var existingHoldings = new List<SamHoldingDocument>
        {
            _fixture.Build<SamHoldingDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.LocationName, "North Market Farm")
                .With(p => p.SecondaryCph, "12/345/7890")
                .With(p => p.SpeciesTypeCode, "CTT")
                .Create(),
            _fixture.Build<SamHoldingDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.LocationName, "North Market Farm")
                .With(p => p.SecondaryCph, "XX/XXX/XXXX")
                .With(p => p.SpeciesTypeCode, "SHP")
                .Create()
        };

        SetupDefaultRepositoryMocks();

        _silverHoldingRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHoldings);

        var step = new SamHoldingImportPersistenceStep(
            _silverHoldingRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHoldingRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverHoldingRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoExistingHoldings_WhenStepExecuted_ShouldInsertNewHoldings()
    {
        var context = _fixture.Build<SamHoldingImportContext>()
            .With(c => c.SilverHoldings, [_fixture.Create<SamHoldingDocument>()])
            .With(c => c.SilverParties, [])
            .With(c => c.SilverPartyRoles, [])
            .Create();

        SetupDefaultRepositoryMocks();

        var sut = new SamHoldingImportPersistenceStep(
            _silverHoldingRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _silverHoldingRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverHoldingRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExistingAndIncomingHoldingsDiffer_WhenStepExecuted_ShouldInsertAndDeleteHoldings()
    {
        var incomingHoldings = new List<SamHoldingDocument>
        {
            _fixture.Build<SamHoldingDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.LocationName, "North Market Farm")
                .With(p => p.SecondaryCph, "12/345/7890")
                .With(p => p.SpeciesTypeCode, "CTT")
                .Create(),
            _fixture.Build<SamHoldingDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.LocationName, "North Market Farm")
                .With(p => p.SecondaryCph, "12/345/7890")
                .With(p => p.SpeciesTypeCode, "SHP")
                .Create(),
            _fixture.Build<SamHoldingDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.LocationName, "North Market Farm")
                .With(p => p.SecondaryCph, "12/345/9876")
                .With(p => p.SpeciesTypeCode, "SHP")
                .Create()
        };

        var existingHoldings = new List<SamHoldingDocument>
        {
            _fixture.Build<SamHoldingDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.LocationName, "North Market Farm")
                .With(p => p.SecondaryCph, "12/345/7890")
                .With(p => p.SpeciesTypeCode, "CTT")
                .Create(),
            _fixture.Build<SamHoldingDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.LocationName, "North Market Farm")
                .With(p => p.SecondaryCph, "XX/XXX/XXXX")
                .With(p => p.SpeciesTypeCode, "SHP")
                .Create()
        };

        var context = _fixture.Build<SamHoldingImportContext>()
            .With(c => c.SilverHoldings, incomingHoldings)
            .With(c => c.SilverParties, [])
            .With(c => c.SilverPartyRoles, [])
            .Create();

        SetupDefaultRepositoryMocks();

        _silverHoldingRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHoldings);

        var sut = new SamHoldingImportPersistenceStep(
            _silverHoldingRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _silverHoldingRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.Is<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(items => items.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
        _silverHoldingRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingPartiesEmpty_WhenStepExecuted_ShouldDeleteOrphans()
    {
        var existingParties = new List<SamPartyDocument>
        {
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000001")
                .Create(),
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000002")
                .Create()
        };

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = []
        };

        SetupDefaultRepositoryMocks();

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindPartyIdsByHoldingIdentifierAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["C1000001", "C1000002"]);

        _silverPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParties);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _silverPartyRepositoryMock.Object,
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExistingPartiesEmpty_WhenStepExecuted_ShouldInsertIncomingParties()
    {
        var incomingParties = _fixture.Build<SamPartyDocument>()
            .With(p => p.PartyId, "C1000001")
            .Create();

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = [incomingParties]
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _silverPartyRepositoryMock.Object,
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.Is<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(items => items.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingAndIncomingPartiesDiffer_WhenStepExecuted_ShouldInsertAndDeleteParties()
    {
        var incomingParties = new List<SamPartyDocument>
        {
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000001")
                .Create(),
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000002")
                .Create(),
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000003")
                .Create()
        };

        var existingParties = new List<SamPartyDocument>
        {
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000001")
                .Create(),
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000099")
                .Create()
        };

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = incomingParties
        };

        SetupDefaultRepositoryMocks();

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindPartyIdsByHoldingIdentifierAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["C1000001", "C1000099"]);

        _silverPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParties);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _silverPartyRepositoryMock.Object,
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.Is<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(items => items.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingSitePartyRolesEmpty_WhenStepExecuted_ShouldDeleteOrphans()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverPartyRoles = []
        };

        var existingSitePartyRoles = new List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>
        {
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.IsHolder, false)
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000001")
                .Create()
        };

        SetupDefaultRepositoryMocks();

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.Silver.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSitePartyRoles);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoExistingSitePartyRoles_WhenStepExecuted_ShouldInsertNewSitePartyRoles()
    {
        var context = _fixture.Build<SamHoldingImportContext>()
            .With(c => c.SilverHoldings, [])
            .With(c => c.SilverParties, [])
            .With(c => c.SilverPartyRoles, [_fixture.Create<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()])
            .Create();

        SetupDefaultRepositoryMocks();

        var sut = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExistingAndIncomingSitePartyRolesDiffer_WhenStepExecuted_ShouldInsertAndDeleteSitePartyRoles()
    {
        var incomingSitePartyRoles = new List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>
        {
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.IsHolder, false)
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000001")
                .Create(),
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.IsHolder, false)
                .With(p => p.PartyId, "C1000002")
                .With(p => p.RoleTypeId, "R1000002")
                .Create(),
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.IsHolder, false)
                .With(p => p.PartyId, "C1000003")
                .With(p => p.RoleTypeId, "R1000003")
                .Create()
        };

        var existingSitePartyRoles = new List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>
        {
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.IsHolder, false)
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000001")
                .Create(),
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.IsHolder, false)
                .With(p => p.PartyId, "C1000099")
                .With(p => p.RoleTypeId, "R1000001")
                .Create()
        };

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverPartyRoles = incomingSitePartyRoles
        };

        SetupDefaultRepositoryMocks();

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.Silver.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSitePartyRoles);

        var sut = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.Is<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(items => items.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingHerdsEmpty_WhenStepExecuted_ShouldDeleteOrphans()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverHerds = []
        };

        var existingHerds = new List<SamHerdDocument>
        {
            _fixture.Build<SamHerdDocument>()
                .With(p => p.CountyParishHoldingHerd, "12/345/6789/01")
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.ProductionUsageCode, "CTT-BEEF")
                .With(p => p.Herdmark, "1000001")
                .Create(),
            _fixture.Build<SamHerdDocument>()
                .With(p => p.CountyParishHoldingHerd, "12/345/6789/01")
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.ProductionUsageCode, "CTT-BEEF")
                .With(p => p.Herdmark, "1000002")
                .Create()
        };

        SetupDefaultRepositoryMocks();

        _silverHerdRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHerdDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHerds);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            _silverHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverHerdRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamHerdDocument>, SamHerdDocument)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoExistingHerds_WhenStepExecuted_ShouldInsertNewHerds()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverHerds = [_fixture.Create<SamHerdDocument>()]
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            _silverHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverHerdRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<SamHerdDocument>, SamHerdDocument)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExistingAndIncomingHerdsDiffer_WhenStepExecuted_ShouldInsertAndDeleteHerds()
    {
        var incomingHerds = new List<SamHerdDocument>
        {
            _fixture.Build<SamHerdDocument>()
                .With(p => p.CountyParishHoldingHerd, "12/345/6789/01")
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.ProductionUsageCode, "CTT-BEEF")
                .With(p => p.Herdmark, "1000001")
                .Create(),
            _fixture.Build<SamHerdDocument>()
                .With(p => p.CountyParishHoldingHerd, "12/345/6789/02")
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.ProductionUsageCode, "CTT-BEEF")
                .With(p => p.Herdmark, "1000002")
                .Create(),
            _fixture.Build<SamHerdDocument>()
                .With(p => p.CountyParishHoldingHerd, "12/345/6789/03")
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.ProductionUsageCode, "CTT-BEEF")
                .With(p => p.Herdmark, "1000003")
                .Create()
        };

        var existingHerds = new List<SamHerdDocument>
        {
            _fixture.Build<SamHerdDocument>()
                .With(p => p.CountyParishHoldingHerd, "12/345/6789/01")
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.ProductionUsageCode, "CTT-BEEF")
                .With(p => p.Herdmark, "1000001")
                .Create(),
            _fixture.Build<SamHerdDocument>()
                .With(p => p.CountyParishHoldingHerd, "12/345/6789/02")
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.ProductionUsageCode, "CTT-BEEF")
                .With(p => p.Herdmark, "9999999")
                .Create()
        };

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverHerds = incomingHerds
        };

        SetupDefaultRepositoryMocks();

        _silverHerdRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHerdDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHerds);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            _silverHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHerdRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.Is<IEnumerable<(FilterDefinition<SamHerdDocument>, SamHerdDocument)>>(items => items.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
        _silverHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // TODO - Add tests for gold data

    private void SetupDefaultRepositoryMocks()
    {
        // Silver Holding
        _silverHoldingRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverHoldingRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHoldingRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Party
        _silverPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Role Relationships
        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.Silver.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindPartyIdsByHoldingIdentifierAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Silver Herds
        _silverHerdRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHerdDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverHerdRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHerdDocument>, SamHerdDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHerdRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site
        _goldSiteRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SiteDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteDocument?)null);

        _goldSiteRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SiteDocument>, SiteDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Party
        _goldPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _goldPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<PartyDocument>, PartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site Party Rol eRelationships
        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site Group Mark Relationships
        _goldSiteGroupMarkRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SiteGroupMarkRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSiteGroupMarkRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SiteGroupMarkRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}