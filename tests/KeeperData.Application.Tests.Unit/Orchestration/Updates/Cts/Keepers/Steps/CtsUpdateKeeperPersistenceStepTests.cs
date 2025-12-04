using AutoFixture;
using FluentAssertions;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers.Steps;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace KeeperData.Application.Tests.Unit.Orchestration.Updates.Cts.Keepers.Steps;

public class CtsUpdateKeeperPersistenceStepTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IGenericRepository<CtsPartyDocument>> _silverPartyRepositoryMock = new();
    private readonly CtsUpdateKeeperPersistenceStep _sut;

    public CtsUpdateKeeperPersistenceStepTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _sut = new CtsUpdateKeeperPersistenceStep(
            _silverPartyRepositoryMock.Object,
            Mock.Of<ILogger<CtsUpdateKeeperPersistenceStep>>());
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenPartyDoesNotExist_ShouldAdd()
    {
        var context = new CtsUpdateKeeperContext
        {
            PartyId = "P1",
            SilverParty = _fixture.Create<CtsPartyDocument>()
        };
        context.SilverParty.PartyId = "P1";

        _silverPartyRepositoryMock
            .Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CtsPartyDocument?)null);

        await _sut.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(x => x.AddAsync(context.SilverParty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenPartyExists_ShouldUpdate()
    {
        var existing = _fixture.Create<CtsPartyDocument>();
        var context = new CtsUpdateKeeperContext
        {
            PartyId = "P1",
            SilverParty = _fixture.Create<CtsPartyDocument>()
        };
        context.SilverParty.PartyId = "P1";

        _silverPartyRepositoryMock
            .Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<CtsPartyDocument, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _sut.ExecuteAsync(context, CancellationToken.None);

        _silverPartyRepositoryMock.Verify(x => x.UpdateAsync(context.SilverParty, It.IsAny<CancellationToken>()), Times.Once);
        context.SilverParty.Id.Should().Be(existing.Id);
    }
}