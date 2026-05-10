using AutoFixture;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Holdings.Steps;

public class SamHoldingImportPersistenceStepTests
{
    private readonly Fixture _fixture;

    private readonly Mock<IGenericRepository<SamHoldingDocument>> _silverHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamHerdDocument>> _silverHerdRepositoryMock = new();

    private readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();
    private readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    private readonly Mock<IGoldSitePartyRoleRelationshipRepository> _goldSitePartyRoleRelationshipRepositoryMock = new();

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
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHoldingRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverHoldingRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHoldingDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverHoldingRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, UpdateDefinition<SamHoldingDocument>)>>(),
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
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _silverHoldingRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverHoldingRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamHoldingDocument>>(items => items.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
        _silverHoldingRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, UpdateDefinition<SamHoldingDocument>)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
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
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _silverHoldingRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamHoldingDocument>>(items => items.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _silverHoldingRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<SamHoldingDocument>, UpdateDefinition<SamHoldingDocument>)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        _silverHoldingRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingPartiesEmpty_WhenStepExecuted_ShouldMakeNoChanges()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = []
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _silverPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverPartyRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, UpdateDefinition<SamPartyDocument>)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _silverPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
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
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamPartyDocument>>(items => items.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, UpdateDefinition<SamPartyDocument>)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingAndIncomingPartiesDiffer_WhenStepExecuted_ShouldInsertNewAndUpdateExistingParties()
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

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = incomingParties
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _silverPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamPartyDocument>>(items => items.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, UpdateDefinition<SamPartyDocument>)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
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
            _silverHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverHerdRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverHerdRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamHerdDocument>, UpdateDefinition<SamHerdDocument>)>>(),
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
            _silverHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverHerdRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamHerdDocument>>(items => items.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
        _silverHerdRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<SamHerdDocument>, UpdateDefinition<SamHerdDocument>)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
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
            _silverHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHerdRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamHerdDocument>>(items => items.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _silverHerdRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<SamHerdDocument>, UpdateDefinition<SamHerdDocument>)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHoldingDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHoldingRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, UpdateDefinition<SamHoldingDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHoldingRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHoldingDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Party
        _silverPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SamPartyDocument?)null);

        _silverPartyRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPartyRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, UpdateDefinition<SamPartyDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Herds
        _silverHerdRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamHerdDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverHerdRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHerdRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHerdDocument>, UpdateDefinition<SamHerdDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHerdRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site
        _goldSiteRepositoryMock
            .Setup(r => r.FindOneByFilterAsync(It.IsAny<FilterDefinition<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SiteDocument?)null);

        _goldSiteRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SiteDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSiteRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SiteDocument>, UpdateDefinition<SiteDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Party
        _goldPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<PartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyDocument?)null);

        _goldPartyRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldPartyRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<PartyDocument>, UpdateDefinition<PartyDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site Party Role Relationships
        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}