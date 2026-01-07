using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Holdings.Steps;

public class SamHoldingImportAggregationStepTests
{
    private readonly Mock<IDataBridgeClient> _clientMock = new();
    private readonly SamHoldingImportAggregationStep _sut;

    public SamHoldingImportAggregationStepTests()
    {
        _sut = new SamHoldingImportAggregationStep(_clientMock.Object, Mock.Of<ILogger<SamHoldingImportAggregationStep>>());
    }

    [Fact]
    public async Task ExecuteCoreAsync_AggregatesData()
    {
        var context = new SamHoldingImportContext { Cph = "12/345/6789" };
        var holding = new SamCphHolding();
        var holder = new SamCphHolder { PARTY_ID = "P1" };
        var herd = new SamHerd { OWNER_PARTY_IDS = "P1", KEEPER_PARTY_IDS = "P2" };
        var party1 = new SamParty { PARTY_ID = "P1" };
        var party2 = new SamParty { PARTY_ID = "P2" };

        _clientMock.Setup(x => x.GetSamHoldingsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([holding]);
        _clientMock.Setup(x => x.GetSamHoldersByCphAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([holder]);
        _clientMock.Setup(x => x.GetSamHerdsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([herd]);

        _clientMock.Setup(x => x.GetSamPartiesAsync(
            It.Is<IEnumerable<string>>(ids => ids.Contains("P1") && ids.Contains("P2")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([party1, party2]);

        await _sut.ExecuteAsync(context, CancellationToken.None);

        context.RawHoldings.Should().Contain(holding);
        context.RawHolders.Should().Contain(holder);
        context.RawHerds.Should().Contain(herd);
        context.RawParties.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenNoHerds_DoesNotFetchParties()
    {
        var context = new SamHoldingImportContext { Cph = "12/345/6789" };

        _clientMock.Setup(x => x.GetSamHoldingsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHoldersByCphAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHerdsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.ExecuteAsync(context, CancellationToken.None);

        _clientMock.Verify(x => x.GetSamPartiesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        context.RawParties.Should().BeEmpty();
    }
}