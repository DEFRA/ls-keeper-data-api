using FluentAssertions;
using KeeperData.Application.Orchestration.Updates.Cts.Agents;
using KeeperData.Application.Orchestration.Updates.Cts.Agents.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Updates.Cts.Agents.Steps;

public class CtsUpdateAgentRawAggregationStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<ILogger<CtsUpdateAgentRawAggregationStep>> _loggerMock = new();
    private readonly CtsUpdateAgentRawAggregationStep _sut;

    public CtsUpdateAgentRawAggregationStepTests()
    {
        _sut = new CtsUpdateAgentRawAggregationStep(_dataBridgeClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPopulateContextWithData_WhenApiReturnsResult()
    {
        var partyId = "P123";
        var factory = new MockCtsRawDataFactory();
        var agent = factory.CreateMockAgentOrKeeper("I", 1, CphGenerator.GenerateFormattedCph());
        agent.PAR_ID = partyId;

        _dataBridgeClientMock
            .Setup(x => x.GetCtsAgentByPartyIdAsync(partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var context = new CtsUpdateAgentContext { PartyId = partyId };

        await _sut.ExecuteAsync(context, CancellationToken.None);

        context.RawAgent.Should().NotBeNull();
        context.RawAgent!.PAR_ID.Should().Be(partyId);
    }
}