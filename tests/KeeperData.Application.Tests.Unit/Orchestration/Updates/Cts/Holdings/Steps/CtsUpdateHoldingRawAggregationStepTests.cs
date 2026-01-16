using FluentAssertions;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Updates.Cts.Holdings.Steps;

public class CtsUpdateHoldingRawAggregationStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<ILogger<CtsUpdateHoldingRawAggregationStep>> _loggerMock = new();
    private readonly CtsUpdateHoldingRawAggregationStep _sut;

    public CtsUpdateHoldingRawAggregationStepTests()
    {
        _sut = new CtsUpdateHoldingRawAggregationStep(_dataBridgeClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPopulateContextWithData_WhenApiReturnsResults()
    {
        // Arrange
        var cph = CphGenerator.GenerateCtsFormattedLidIdentifier("AH");
        var factory = new MockCtsRawDataFactory();

        var holding = factory.CreateMockHolding("I", 1, cph);
        var agent = factory.CreateMockAgentOrKeeper("I", 1, cph);
        var keeper = factory.CreateMockAgentOrKeeper("I", 1, cph);

        _dataBridgeClientMock
            .Setup(x => x.GetCtsHoldingsAsync(cph, It.IsAny<CancellationToken>()))
            .ReturnsAsync([holding]);

        _dataBridgeClientMock
            .Setup(x => x.GetCtsAgentsAsync(cph, It.IsAny<CancellationToken>()))
            .ReturnsAsync([agent]);

        _dataBridgeClientMock
            .Setup(x => x.GetCtsKeepersAsync(cph, It.IsAny<CancellationToken>()))
            .ReturnsAsync([keeper]);

        var context = new CtsUpdateHoldingContext { Cph = cph };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.RawHolding.Should().NotBeNull();
        context.RawHolding!.LID_FULL_IDENTIFIER.Should().Be(cph);

        context.RawAgents.Should().ContainSingle();
        context.RawKeepers.Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldHandleEmptyResults_Gracefully()
    {
        // Arrange
        var cph = "AH-MISSING";

        _dataBridgeClientMock.Setup(x => x.GetCtsHoldingsAsync(cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _dataBridgeClientMock.Setup(x => x.GetCtsAgentsAsync(cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _dataBridgeClientMock.Setup(x => x.GetCtsKeepersAsync(cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var context = new CtsUpdateHoldingContext { Cph = cph };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.RawHolding.Should().BeNull();
        context.RawAgents.Should().BeEmpty();
        context.RawKeepers.Should().BeEmpty();
    }
}