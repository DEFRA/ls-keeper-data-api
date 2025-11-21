using AutoFixture;
using KeeperData.Application.Orchestration.Imports.Sam.Holders;
using KeeperData.Application.Orchestration.Imports.Sam.Holders.Steps;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Holders.Steps;

public class SamHolderImportPersistenceStepTests
{
    private readonly Fixture _fixture;

    private readonly Mock<IGenericRepository<SamPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<ISilverSitePartyRoleRelationshipRepository> _silverSitePartyRoleRelationshipRepositoryMock = new();

    private readonly Mock<IGenericRepository<PartyDocument>> _goldPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>> _goldSitePartyRoleRelationshipRepositoryMock = new();

    public SamHolderImportPersistenceStepTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenIncomingHoldersEmpty_WhenStepExecuted_ShouldNotEffectChange()
    {
        var context = new SamHolderImportContext
        {
            PartyId = "C1000001",
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = []
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHolderImportPersistenceStep(
            _silverPartyRepositoryMock.Object,
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<SamHolderImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.FindOneAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoExistingHolders_WhenStepExecuted_ShouldInsertNewHolder()
    {
        var incomingParty = _fixture.Build<SamPartyDocument>()
            .With(p => p.PartyId, "C1000001")
            .Create();

        var context = new SamHolderImportContext
        {
            PartyId = "C1000001",
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = [incomingParty]
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHolderImportPersistenceStep(
            _silverPartyRepositoryMock.Object,
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<SamHolderImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(r => r.FindOneAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverPartyRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(It.Is<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(items => items.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingSitePartyRolesEmpty_WhenStepExecuted_ShouldDeleteOrphans()
    {
        var incomingParties = new List<SamPartyDocument>
        {
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000001")
                .Create()
        };

        var context = new SamHolderImportContext
        {
            PartyId = "C1000001",
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = incomingParties,
            SilverPartyRoles = []
        };

        var existingSitePartyRoles = new List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>
        {
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000001")
                .Create()
        };

        SetupDefaultRepositoryMocks();

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.Silver.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSitePartyRoles);

        var step = new SamHolderImportPersistenceStep(
            _silverPartyRepositoryMock.Object,
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<SamHolderImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.IsAny<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoExistingSitePartyRoles_WhenStepExecuted_ShouldInsertNewSitePartyRoles()
    {
        var incomingParties = new List<SamPartyDocument>
        {
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000001")
                .Create()
        };

        var incomingSitePartyRoles = new List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>
        {
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000001")
                .Create(),
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000002")
                .Create()
        };

        var context = new SamHolderImportContext
        {
            PartyId = "C1000001",
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = incomingParties,
            SilverPartyRoles = incomingSitePartyRoles
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHolderImportPersistenceStep(
            _silverPartyRepositoryMock.Object,
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<SamHolderImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(items => items.Count() == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExistingAndIncomingSitePartyRolesDiffer_WhenStepExecuted_ShouldInsertAndDeleteSitePartyRoles()
    {
        var incomingParties = new List<SamPartyDocument>
        {
            _fixture.Build<SamPartyDocument>()
                .With(p => p.PartyId, "C1000001")
                .Create()
        };

        var incomingSitePartyRoles = new List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>
        {
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000001")
                .Create(),
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000002")
                .Create(),
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000003")
                .Create()
        };

        var existingSitePartyRoles = new List<Core.Documents.Silver.SitePartyRoleRelationshipDocument>
        {
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R1000001")
                .Create(),
            _fixture.Build<Core.Documents.Silver.SitePartyRoleRelationshipDocument>()
                .With(p => p.Source, "SAM")
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.PartyId, "C1000001")
                .With(p => p.RoleTypeId, "R10000XX")
                .Create()
        };

        var context = new SamHolderImportContext
        {
            PartyId = "C1000001",
            CurrentDateTime = DateTime.UtcNow,
            SilverParties = incomingParties,
            SilverPartyRoles = incomingSitePartyRoles
        };

        SetupDefaultRepositoryMocks();

        _silverSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.Silver.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSitePartyRoles);

        var step = new SamHolderImportPersistenceStep(
            _silverPartyRepositoryMock.Object,
            _silverSitePartyRoleRelationshipRepositoryMock.Object,
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<Core.Documents.SitePartyRoleRelationshipDocument>>(),
            Mock.Of<ILogger<SamHolderImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverSitePartyRoleRelationshipRepositoryMock.Verify(r => r.BulkUpsertWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<Core.Documents.Silver.SitePartyRoleRelationshipDocument>, Core.Documents.Silver.SitePartyRoleRelationshipDocument)>>(items => items.Count() == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // TODO - Add tests for gold data

    private void SetupDefaultRepositoryMocks()
    {
        // Silver Party
        _silverPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<SamPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SamPartyDocument?)null);

        _silverPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPartyDocument>, SamPartyDocument)>>(), It.IsAny<CancellationToken>()))
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

        // Gold Party
        _goldPartyRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<PartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyDocument?)null);

        _goldPartyRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<PartyDocument>, PartyDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Gold Site Party Role Relationships
        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Core.Documents.SitePartyRoleRelationshipDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldSitePartyRoleRelationshipRepositoryMock
            .Setup(r => r.BulkUpsertWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<Core.Documents.SitePartyRoleRelationshipDocument>, Core.Documents.SitePartyRoleRelationshipDocument)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}