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
    private readonly Mock<IGenericRepository<PartyRoleRelationshipDocument>> _silverPartyRoleRelationshipRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamHerdDocument>> _silverHerdRepositoryMock = new();

    private readonly Mock<IGenericRepository<SiteDocument>> _goldSiteRepositoryMock = new();
    private readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<SitePartyRoleRelationshipDocument>> _goldSitePartyRoleRelationshipRepositoryMock = new();
    private readonly Mock<IGenericRepository<SiteGroupMarkRelationshipDocument>> _goldSiteGroupMarkRelationshipRepositoryMock = new();

    public SamHoldingImportPersistenceStepTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenNoExistingHolding_WhenStepExecuted_ShouldInsertNewHolding()
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
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _silverHoldingRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingPartiesEmpty_WhenStepExecuted_ShouldDeleteExisting()
    {
        var existingParties = _fixture.Build<SamPartyDocument>()
            .With(p => p.PartyId, "P2") // Orphan
            .With(p => p.CountyParishHoldingNumber, "CPH1")
            .Create();

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = []
        };

        SetupDefaultRepositoryMocks();

        _silverPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingParties]);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _silverPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<SitePartyRoleRelationshipDocument>>(),
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
            .With(p => p.PartyId, "P1")
            .With(p => p.CountyParishHoldingNumber, "CPH1")
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
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingAndIncomingPartiesDiffer_WhenStepExecuted_ShouldInsertAndDeleteParties()
    {
        var incomingParties = _fixture.Build<SamPartyDocument>()
            .With(p => p.PartyId, "P1")
            .With(p => p.CountyParishHoldingNumber, "CPH1")
            .Create();

        var existingParties = _fixture.Build<SamPartyDocument>()
            .With(p => p.PartyId, "P2") // Orphan
            .With(p => p.CountyParishHoldingNumber, "CPH1")
            .Create();

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = [incomingParties]
        };

        SetupDefaultRepositoryMocks();

        _silverPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingParties]);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _silverPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingPartyRolesEmpty_WhenStepExecuted_ShouldDeleteExisting()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverPartyRoles = []
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverPartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRoleRelationshipRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingPartyRoles_WhenStepExecuted_ShouldReplaceAllPartyRoles()
    {
        var roles = _fixture.CreateMany<PartyRoleRelationshipDocument>(3).ToList();

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverPartyRoles = roles
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _silverPartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRoleRelationshipRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<PartyRoleRelationshipDocument>>(x => x.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingHerdsEmpty_WhenStepExecuted_ShouldDeleteExisting()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverHerds = []
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            _silverHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverHerdRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingHerds_WhenStepExecuted_ShouldReplaceAllHerds()
    {
        var herds = _fixture.CreateMany<SamHerdDocument>(3).ToList();

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            CurrentDateTime = DateTime.UtcNow,
            SilverHerds = herds
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            _silverHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<SitePartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SiteGroupMarkRelationshipDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverHerdRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamHerdDocument>>(x => x.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    // TODO - Add tests for gold data

    private void SetupDefaultRepositoryMocks()
    {
        // Silver Holding
        _silverHoldingRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SamHoldingDocument?)null);

        _silverHoldingRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Party
        _silverPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Role Relationships
        _silverPartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPartyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Silver Herds
        _silverHerdRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverHerdRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
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
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
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