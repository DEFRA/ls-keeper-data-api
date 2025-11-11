using AutoFixture;
using KeeperData.Application.Orchestration.Cts.Holdings;
using KeeperData.Application.Orchestration.Cts.Holdings.Steps;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Cts.Holdings.Steps;

public class CtsHoldingImportPersistenceStepTests
{
    private readonly Fixture _fixture;

    private readonly Mock<IGenericRepository<CtsHoldingDocument>> _ctsHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<CtsPartyDocument>> _ctsPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument>> _partyRoleRelationshipRepositoryMock = new();

    public CtsHoldingImportPersistenceStepTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenNoExistingHolding_WhenStepExecuted_ShouldInsertNewHolding()
    {
        var context = _fixture.Build<CtsHoldingImportContext>()
            .With(c => c.SilverHoldings, [_fixture.Create<CtsHoldingDocument>()])
            .With(c => c.SilverParties, [])
            .With(c => c.SilverPartyRoles, [])
            .Create();

        SetupDefaultRepositoryMocks();

        var sut = new CtsHoldingImportPersistenceStep(
            _ctsHoldingRepositoryMock.Object,
            Mock.Of<IGenericRepository<CtsPartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<CtsHoldingImportPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        _ctsHoldingRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<CtsHoldingDocument>, CtsHoldingDocument)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingPartiesEmpty_WhenStepExecuted_ShouldDeleteExisting()
    {
        var existingParties = _fixture.Build<CtsPartyDocument>()
            .With(p => p.PartyId, "P2") // Orphan
            .With(p => p.CountyParishHoldingNumber, "CPH1")
            .Create();

        var context = new CtsHoldingImportContext
        {
            Cph = CphGenerator.GenerateCtsFormattedLidIdentifier("AH"),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = []
        };

        SetupDefaultRepositoryMocks();

        _ctsPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingParties]);

        var step = new CtsHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<CtsHoldingDocument>>(),
            _ctsPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<CtsHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _ctsPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsPartyDocument>, CtsPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Never);
        _ctsPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExistingPartiesEmpty_WhenStepExecuted_ShouldInsertIncomingParties()
    {
        var incomingParties = _fixture.Build<CtsPartyDocument>()
            .With(p => p.PartyId, "P1")
            .With(p => p.CountyParishHoldingNumber, "CPH1")
            .Create();

        var context = new CtsHoldingImportContext
        {
            Cph = CphGenerator.GenerateCtsFormattedLidIdentifier("AH"),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = [incomingParties]
        };

        SetupDefaultRepositoryMocks();

        var step = new CtsHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<CtsHoldingDocument>>(),
            _ctsPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<CtsHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _ctsPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsPartyDocument>, CtsPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Once);
        _ctsPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingAndIncomingPartiesDiffer_WhenStepExecuted_ShouldInsertAndDeleteParties()
    {
        var incomingParties = _fixture.Build<CtsPartyDocument>()
            .With(p => p.PartyId, "P1")
            .With(p => p.CountyParishHoldingNumber, "CPH1")
            .Create();

        var existingParties = _fixture.Build<CtsPartyDocument>()
            .With(p => p.PartyId, "P2") // Orphan
            .With(p => p.CountyParishHoldingNumber, "CPH1")
            .Create();

        var context = new CtsHoldingImportContext
        {
            Cph = CphGenerator.GenerateCtsFormattedLidIdentifier("AH"),
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = [incomingParties]
        };

        SetupDefaultRepositoryMocks();

        _ctsPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingParties]);

        var step = new CtsHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<CtsHoldingDocument>>(),
            _ctsPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<CtsHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _ctsPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsPartyDocument>, CtsPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Once);
        _ctsPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingPartyRolesEmpty_WhenStepExecuted_ShouldDeleteExisting()
    {
        var context = new CtsHoldingImportContext
        {
            Cph = CphGenerator.GenerateCtsFormattedLidIdentifier("AH"),
            CurrentDateTime = DateTime.UtcNow,
            SilverPartyRoles = []
        };

        SetupDefaultRepositoryMocks();

        var step = new CtsHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<CtsHoldingDocument>>(),
            Mock.Of<IGenericRepository<CtsPartyDocument>>(),
            _partyRoleRelationshipRepositoryMock.Object,
            Mock.Of<ILogger<CtsHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _partyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _partyRoleRelationshipRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(x => x.Count() == 3), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingPartyRoles_WhenStepExecuted_ShouldReplaceAllPartyRoles()
    {
        var roles = _fixture.CreateMany<Core.Documents.Silver.SitePartyRoleRelationshipDocument>(3).ToList();

        var context = new CtsHoldingImportContext
        {
            Cph = CphGenerator.GenerateCtsFormattedLidIdentifier("AH"),
            CurrentDateTime = DateTime.UtcNow,
            SilverPartyRoles = roles
        };

        SetupDefaultRepositoryMocks();

        var step = new CtsHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<CtsHoldingDocument>>(),
            Mock.Of<IGenericRepository<CtsPartyDocument>>(),
            _partyRoleRelationshipRepositoryMock.Object,
            Mock.Of<ILogger<CtsHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _partyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _partyRoleRelationshipRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(x => x.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupDefaultRepositoryMocks()
    {
        // Holding
        _ctsHoldingRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<CtsHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CtsHoldingDocument?)null);

        _ctsHoldingRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsHoldingDocument>, CtsHoldingDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Party
        _ctsPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _ctsPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsPartyDocument>, CtsPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _ctsPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // RoleRelationships
        _partyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _partyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}