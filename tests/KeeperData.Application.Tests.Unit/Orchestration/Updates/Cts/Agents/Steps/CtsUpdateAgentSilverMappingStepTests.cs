using FluentAssertions;
using KeeperData.Application.Orchestration.Updates.Cts.Agents;
using KeeperData.Application.Orchestration.Updates.Cts.Agents.Steps;
using KeeperData.Core.Services;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Updates.Cts.Agents.Steps;

public class CtsUpdateAgentSilverMappingStepTests
{
    private readonly Mock<IRoleTypeLookupService> _roleTypeLookupServiceMock = new();
    private readonly Mock<ILogger<CtsUpdateAgentSilverMappingStep>> _loggerMock = new();
    private readonly CtsUpdateAgentSilverMappingStep _sut;

    public CtsUpdateAgentSilverMappingStepTests()
    {
        _roleTypeLookupServiceMock
            .Setup(x => x.FindAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid.NewGuid().ToString(), "Agent"));

        _sut = new CtsUpdateAgentSilverMappingStep(_roleTypeLookupServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldMapRawDataToSilver()
    {
        var factory = new MockCtsRawDataFactory();
        var agent = factory.CreateMockAgentOrKeeper("I", 1, CphGenerator.GenerateFormattedCph());

        var context = new CtsUpdateAgentContext
        {
            PartyId = agent.PAR_ID,
            CurrentDateTime = DateTime.UtcNow,
            RawAgent = agent
        };

        await _sut.ExecuteAsync(context, CancellationToken.None);

        context.SilverParty.Should().NotBeNull();
        context.SilverParty!.PartyId.Should().Be(agent.PAR_ID);
        context.SilverPartyRoles.Should().ContainSingle();
    }
}