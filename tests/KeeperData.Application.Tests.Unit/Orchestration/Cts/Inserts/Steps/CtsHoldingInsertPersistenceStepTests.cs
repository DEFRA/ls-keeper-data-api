using AutoFixture;
using KeeperData.Application.Orchestration.Cts.Inserts;
using KeeperData.Application.Orchestration.Cts.Inserts.Steps;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Cts.Inserts.Steps;

public class CtsHoldingInsertPersistenceStepTests
{
    private readonly Fixture _fixture;

    public CtsHoldingInsertPersistenceStepTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenNoExistingHolding_WhenStepExecuted_ShouldInsertNewHolding()
    {
        var context = _fixture.Build<CtsHoldingInsertContext>()
            .With(c => c.SilverHoldings, [_fixture.Create<CtsHoldingDocument>()])
            .With(c => c.SilverParties, [])
            .With(c => c.SilverPartyRoles, [])
            .Create();

        var ctsHoldingRepositoryMock = new Mock<IGenericRepository<CtsHoldingDocument>>();
        ctsHoldingRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<CtsHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CtsHoldingDocument?)null);

        ctsHoldingRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsHoldingDocument>, CtsHoldingDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new CtsHoldingInsertPersistenceStep(
            ctsHoldingRepositoryMock.Object,
            Mock.Of<IGenericRepository<CtsPartyDocument>>(),
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<CtsHoldingInsertPersistenceStep>>());

        await sut.ExecuteAsync(context, CancellationToken.None);

        ctsHoldingRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<CtsHoldingDocument>, CtsHoldingDocument)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
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

        var context = new CtsHoldingInsertContext
        {
            Cph = Guid.NewGuid().ToString(),
            SilverParties = [incomingParties]
        };

        var ctsPartyRepositoryMock = new Mock<IGenericRepository<CtsPartyDocument>>();
        ctsPartyRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingParties]);

        ctsPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsPartyDocument>, CtsPartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ctsPartyRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new CtsHoldingInsertPersistenceStep(
            Mock.Of<IGenericRepository<CtsHoldingDocument>>(),
            ctsPartyRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<CtsHoldingInsertPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        ctsPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<CtsPartyDocument>, CtsPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Once);
        ctsPartyRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExistingPartyRoles_WhenStepExecuted_ShouldReplaceAllPartyRoles()
    {
        var roles = _fixture.CreateMany<PartyRoleRelationshipDocument>(3).ToList();

        var context = new CtsHoldingInsertContext
        {
            Cph = Guid.NewGuid().ToString(),
            SilverPartyRoles = roles
        };

        var partyRoleRelationshipRepositoryMock = new Mock<IGenericRepository<PartyRoleRelationshipDocument>>();
        partyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        partyRoleRelationshipRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new CtsHoldingInsertPersistenceStep(
            Mock.Of<IGenericRepository<CtsHoldingDocument>>(),
            Mock.Of<IGenericRepository<CtsPartyDocument>>(),
            partyRoleRelationshipRepositoryMock.Object,
            Mock.Of<ILogger<CtsHoldingInsertPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        partyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        partyRoleRelationshipRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<PartyRoleRelationshipDocument>>(x => x.Count() == 3), It.IsAny<CancellationToken>()), Times.Once);
    }
}