using FluentAssertions;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Updates.Cts.Keepers.Steps;

public class CtsUpdateKeeperRawAggregationStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<ILogger<CtsUpdateKeeperRawAggregationStep>> _loggerMock = new();
    private readonly CtsUpdateKeeperRawAggregationStep _sut;

    public CtsUpdateKeeperRawAggregationStepTests()
    {
        _sut = new CtsUpdateKeeperRawAggregationStep(_dataBridgeClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPopulateContextWithData_WhenApiReturnsResult()
    {
        var partyId = "P123";
        var factory = new MockCtsRawDataFactory();
        var keeper = factory.CreateMockAgentOrKeeper("I", 1, CphGenerator.GenerateFormattedCph());
        keeper.PAR_ID = partyId;

        _dataBridgeClientMock
            .Setup(x => x.GetCtsKeeperByPartyIdAsync(partyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(keeper);

        var context = new CtsUpdateKeeperContext { PartyId = partyId };

        await _sut.ExecuteAsync(context, CancellationToken.None);

        context.RawKeeper.Should().NotBeNull();
        context.RawKeeper!.PAR_ID.Should().Be(partyId);
    }
}