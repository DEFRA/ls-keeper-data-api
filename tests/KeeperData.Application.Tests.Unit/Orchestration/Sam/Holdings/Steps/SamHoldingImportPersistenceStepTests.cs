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

    private readonly Mock<IGenericRepository<SamHoldingDocument>> _samHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamPartyDocument>> _samPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<PartyRoleRelationshipDocument>> _partyRoleRelationshipRepositoryMock = new();
    private readonly Mock<IGenericRepository<SamHerdDocument>> _samHerdRepositoryMock = new();

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
            _samHoldingRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _samHoldingRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
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
            SilverParties = []
        };

        SetupDefaultRepositoryMocks();

        _samPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingParties]);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _samPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _samPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Never);
        _samPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
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
            SilverParties = [incomingParties]
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _samPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _samPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Once);
        _samPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
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
            SilverParties = [incomingParties]
        };

        SetupDefaultRepositoryMocks();

        _samPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingParties]);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            _samPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _samPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Once);
        _samPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingPartyRolesEmpty_WhenStepExecuted_ShouldDeleteExisting()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            SilverPartyRoles = []
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _partyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _partyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _partyRoleRelationshipRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingPartyRoles_WhenStepExecuted_ShouldReplaceAllPartyRoles()
    {
        var roles = _fixture.CreateMany<PartyRoleRelationshipDocument>(3).ToList();

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            SilverPartyRoles = roles
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            _partyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _partyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _partyRoleRelationshipRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<PartyRoleRelationshipDocument>>(x => x.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingHerdsEmpty_WhenStepExecuted_ShouldDeleteExisting()
    {
        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            SilverHerds = []
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            _samHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _samHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _samHerdRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingHerds_WhenStepExecuted_ShouldReplaceAllHerds()
    {
        var herds = _fixture.CreateMany<SamHerdDocument>(3).ToList();

        var context = new SamHoldingImportContext
        {
            Cph = Guid.NewGuid().ToString(),
            SilverHerds = herds
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            _samHerdRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _samHerdRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _samHerdRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamHerdDocument>>(x => x.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupDefaultRepositoryMocks()
    {
        // Holding
        _samHoldingRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SamHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SamHoldingDocument?)null);

        _samHoldingRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamHoldingDocument>, SamHoldingDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Party
        _samPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _samPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _samPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // RoleRelationships
        _partyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _partyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Herds
        _samHerdRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _samHerdRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamHerdDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}