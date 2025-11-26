using AutoFixture;
using FluentAssertions;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings.Steps;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Updates.Cts.Holdings.Steps;

public class CtsUpdateHoldingPersistenceStepTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IGenericRepository<CtsHoldingDocument>> _silverHoldingRepositoryMock = new();
    private readonly Mock<IGenericRepository<CtsPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly Mock<IGenericRepository<SitePartyRoleRelationshipDocument>> _silverPartyRoleRelationshipRepositoryMock = new();
    private readonly Mock<ILogger<CtsUpdateHoldingPersistenceStep>> _loggerMock = new();

    private readonly CtsUpdateHoldingPersistenceStep _sut;

    public CtsUpdateHoldingPersistenceStepTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _sut = new CtsUpdateHoldingPersistenceStep(
            _silverHoldingRepositoryMock.Object,
            _silverPartyRepositoryMock.Object,
            _silverPartyRoleRelationshipRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenHoldingDoesNotExist_ShouldAdd()
    {
        // Arrange
        var context = new CtsUpdateHoldingContext
        {
            Cph = "AH-123",
            SilverHolding = _fixture.Create<CtsHoldingDocument>()
        };
        context.SilverHolding.CountyParishHoldingNumber = context.CphTrimmed;

        _silverHoldingRepositoryMock
            .Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<CtsHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CtsHoldingDocument?)null);

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _silverHoldingRepositoryMock.Verify(x => x.AddAsync(context.SilverHolding, It.IsAny<CancellationToken>()), Times.Once);
        _silverHoldingRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<CtsHoldingDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenHoldingExists_ShouldUpdate()
    {
        // Arrange
        var existingHolding = _fixture.Create<CtsHoldingDocument>();
        var context = new CtsUpdateHoldingContext
        {
            Cph = "AH-123",
            SilverHolding = _fixture.Create<CtsHoldingDocument>()
        };
        context.SilverHolding.CountyParishHoldingNumber = context.CphTrimmed;

        _silverHoldingRepositoryMock
            .Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<CtsHoldingDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHolding);

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _silverHoldingRepositoryMock.Verify(x => x.UpdateAsync(context.SilverHolding, It.IsAny<CancellationToken>()), Times.Once);
        _silverHoldingRepositoryMock.Verify(x => x.AddAsync(It.IsAny<CtsHoldingDocument>(), It.IsAny<CancellationToken>()), Times.Never);

        context.SilverHolding.Id.Should().Be(existingHolding.Id);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldDeleteOrphanedParties()
    {
        // Arrange
        var context = new CtsUpdateHoldingContext
        {
            Cph = "AH-123",
            SilverParties = []
        };

        var existingParties = new List<CtsPartyDocument>
        {
            _fixture.Build<CtsPartyDocument>()
                .With(x => x.PartyId, "P1")
                .With(x => x.CountyParishHoldingNumber, context.CphTrimmed)
                .Create()
        };

        _silverPartyRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingParties);

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _silverPartyRepositoryMock.Verify(x => x.DeleteManyAsync(It.IsAny<FilterDefinition<CtsPartyDocument>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldReplacePartyRoleRelationships()
    {
        // Arrange
        var context = new CtsUpdateHoldingContext
        {
            Cph = "AH-123",
            SilverPartyRoles = _fixture.CreateMany<SitePartyRoleRelationshipDocument>(2).ToList()
        };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        _silverPartyRoleRelationshipRepositoryMock.Verify(x => x.DeleteManyAsync(
            It.IsAny<FilterDefinition<SitePartyRoleRelationshipDocument>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Add new
        _silverPartyRoleRelationshipRepositoryMock.Verify(x => x.AddManyAsync(
            context.SilverPartyRoles,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}