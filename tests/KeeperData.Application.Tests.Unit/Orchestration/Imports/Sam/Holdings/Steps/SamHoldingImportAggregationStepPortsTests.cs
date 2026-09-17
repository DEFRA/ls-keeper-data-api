using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Sam.Holdings.Steps;

public class SamHoldingImportAggregationStepPortsTests
{
    private readonly Mock<IDataBridgeClient> _clientMock = new();
    private readonly SamHoldingImportAggregationStep _sut;

    public SamHoldingImportAggregationStepPortsTests()
    {
        _sut = new SamHoldingImportAggregationStep(_clientMock.Object, Mock.Of<ILogger<SamHoldingImportAggregationStep>>());
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldAggregatePortsData()
    {
        var context = new SamHoldingImportContext { Cph = "12/345/6789" };
        var port = new SamPort { CPH = "12/345/6789", PREMISES_NAME = "Test Port" };

        _clientMock.Setup(x => x.GetSamHoldingsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHoldersByCphAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHerdsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamPortsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([port]);

        await _sut.ExecuteAsync(context, CancellationToken.None);

        context.RawPorts.Should().Contain(port);
        context.RawPorts.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenNoPorts_ShouldReturnEmptyList()
    {
        var context = new SamHoldingImportContext { Cph = "12/345/6789" };

        _clientMock.Setup(x => x.GetSamHoldingsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHoldersByCphAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHerdsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamPortsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.ExecuteAsync(context, CancellationToken.None);

        context.RawPorts.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenPortsClientThrowsRetryableException_PropagatesException()
    {
        // Arrange
        var context = new SamHoldingImportContext { Cph = "12/345/6789" };
        var exception = new RetryableException("Transient error");

        _clientMock.Setup(x => x.GetSamHoldingsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHoldersByCphAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHerdsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamPortsAsync(context.Cph, It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        // Act
        var act = () => _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RetryableException>().WithMessage("Transient error");
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenPortsClientThrowsNonRetryableException_PropagatesException()
    {
        // Arrange
        var context = new SamHoldingImportContext { Cph = "12/345/6789" };
        var exception = new NonRetryableException("Permanent error");

        _clientMock.Setup(x => x.GetSamHoldingsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHoldersByCphAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHerdsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamPortsAsync(context.Cph, It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        // Act
        var act = () => _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NonRetryableException>().WithMessage("Permanent error");
    }

    [Fact]
    public async Task ExecuteCoreAsync_WithMultiplePorts_ShouldAggregateAllPorts()
    {
        var context = new SamHoldingImportContext { Cph = "12/345/6789" };
        var ports = new List<SamPort>
        {
            new() { CPH = "12/345/6789", PREMISES_NAME = "Port A" },
            new() { CPH = "12/345/6789", PREMISES_NAME = "Port B" },
            new() { CPH = "12/345/6789", PREMISES_NAME = "Port C" }
        };

        _clientMock.Setup(x => x.GetSamHoldingsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHoldersByCphAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamHerdsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _clientMock.Setup(x => x.GetSamPortsAsync(context.Cph, It.IsAny<CancellationToken>())).ReturnsAsync(ports);

        await _sut.ExecuteAsync(context, CancellationToken.None);

        context.RawPorts.Should().HaveCount(3);
        context.RawPorts.Should().Contain(p => p.PREMISES_NAME == "Port A");
        context.RawPorts.Should().Contain(p => p.PREMISES_NAME == "Port B");
        context.RawPorts.Should().Contain(p => p.PREMISES_NAME == "Port C");
    }
}