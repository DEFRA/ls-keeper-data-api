using AutoFixture;
using FluentAssertions;
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

public class SamHoldingImportPersistenceStepPortsTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IGenericRepository<SamPortDocument>> _silverPortRepositoryMock = new();
    private readonly Mock<IGenericRepository<PortDocument>> _goldPortRepositoryMock = new();

    public SamHoldingImportPersistenceStepPortsTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public async Task GivenIncomingPortsEmpty_WhenStepExecuted_ShouldDeleteOrphans()
    {
        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            CurrentDateTime = DateTime.UtcNow,
            SilverPorts = []
        };

        var existingPorts = new List<SamPortDocument>
        {
            _fixture.Build<SamPortDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.PremisesName, "Test Port A")
                .Create(),
            _fixture.Build<SamPortDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.PremisesName, "Test Port B")
                .Create()
        };

        SetupDefaultRepositoryMocks();

        _silverPortRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPortDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPorts);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            _silverPortRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<PortDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPortRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPortDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _silverPortRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<SamPortDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoExistingPorts_WhenStepExecuted_ShouldInsertNewPorts()
    {
        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            CurrentDateTime = DateTime.UtcNow,
            SilverPorts = [_fixture.Build<SamPortDocument>().With(p => p.CountyParishHoldingNumber, "12/345/6789").Create()]
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            _silverPortRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<PortDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPortRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPortDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _silverPortRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamPortDocument>>(items => items.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenExistingAndIncomingPortsDiffer_WhenStepExecuted_ShouldInsertAndDeletePorts()
    {
        var incomingPorts = new List<SamPortDocument>
        {
            _fixture.Build<SamPortDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.PremisesName, "Port A")
                .Create(),
            _fixture.Build<SamPortDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.PremisesName, "Port B")
                .Create()
        };

        var existingPorts = new List<SamPortDocument>
        {
            _fixture.Build<SamPortDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.PremisesName, "Port A")
                .Create(),
            _fixture.Build<SamPortDocument>()
                .With(p => p.CountyParishHoldingNumber, "12/345/6789")
                .With(p => p.PremisesName, "Port C")
                .Create()
        };

        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            CurrentDateTime = DateTime.UtcNow,
            SilverPorts = incomingPorts
        };

        SetupDefaultRepositoryMocks();

        _silverPortRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPortDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPorts);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            _silverPortRepositoryMock.Object,
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            Mock.Of<IGenericRepository<PortDocument>>(),
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _silverPortRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<SamPortDocument>>(items => items.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
        _silverPortRepositoryMock.Verify(r => r.BulkUpdateWithCustomFilterAsync(
            It.Is<IEnumerable<(FilterDefinition<SamPortDocument>, UpdateDefinition<SamPortDocument>)>>(items => items.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        _silverPortRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPortDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenIncomingGoldPortsEmpty_WhenStepExecuted_ShouldDeleteOrphans()
    {
        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            CurrentDateTime = DateTime.UtcNow,
            GoldPorts = []
        };

        var existingPorts = new List<PortDocument>
        {
            _fixture.Build<PortDocument>()
                .With(p => p.HoldingIdentifier, "12/345/6789")
                .With(p => p.Name, "Gold Port A")
                .Create()
        };

        SetupDefaultRepositoryMocks();

        _goldPortRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PortDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPorts);

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SamPortDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            _goldPortRepositoryMock.Object,
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _goldPortRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PortDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
        _goldPortRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<PortDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoExistingGoldPorts_WhenStepExecuted_ShouldInsertNewPorts()
    {
        var context = new SamHoldingImportContext
        {
            Cph = "12/345/6789",
            CurrentDateTime = DateTime.UtcNow,
            GoldPorts = [_fixture.Build<PortDocument>().With(p => p.HoldingIdentifier, "12/345/6789").Create()]
        };

        SetupDefaultRepositoryMocks();

        var step = new SamHoldingImportPersistenceStep(
            Mock.Of<IGenericRepository<SamHoldingDocument>>(),
            Mock.Of<IGenericRepository<SamPartyDocument>>(),
            Mock.Of<IGenericRepository<SamHerdDocument>>(),
            Mock.Of<IGenericRepository<SamPortDocument>>(),
            Mock.Of<IGenericRepository<SiteDocument>>(),
            Mock.Of<IGenericRepository<PartyDocument>>(),
            _goldPortRepositoryMock.Object,
            Mock.Of<IGoldSitePartyRoleRelationshipRepository>(),
            Mock.Of<ILogger<SamHoldingImportPersistenceStep>>());

        await step.ExecuteAsync(context, CancellationToken.None);

        _goldPortRepositoryMock.Verify(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PortDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
        _goldPortRepositoryMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<PortDocument>>(items => items.Count() == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupDefaultRepositoryMocks()
    {
        _silverPortRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SamPortDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _silverPortRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<SamPortDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPortRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<SamPortDocument>, UpdateDefinition<SamPortDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _silverPortRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<SamPortDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldPortRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PortDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _goldPortRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<PortDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldPortRepositoryMock
            .Setup(r => r.BulkUpdateWithCustomFilterAsync(It.IsAny<IEnumerable<(FilterDefinition<PortDocument>, UpdateDefinition<PortDocument>)>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _goldPortRepositoryMock
            .Setup(r => r.DeleteManyAsync(It.IsAny<FilterDefinition<PortDocument>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}