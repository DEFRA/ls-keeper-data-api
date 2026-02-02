using FluentAssertions;
using KeeperData.Application.Orchestration.Imports.Cts.Holdings;
using KeeperData.Application.Orchestration.Imports.Cts.Holdings.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Exceptions;
using KeeperData.Tests.Common.Factories;
using KeeperData.Tests.Common.Generators;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.Imports.Cts.Holdings.Steps;

public class CtsHoldingImportRawAggregationStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<ILogger<CtsHoldingImportRawAggregationStep>> _loggerMock = new();
    private readonly CtsHoldingImportRawAggregationStep _sut;

    public CtsHoldingImportRawAggregationStepTests()
    {
        _sut = new CtsHoldingImportRawAggregationStep(_dataBridgeClientMock.Object, _loggerMock.Object);
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

        var context = new CtsHoldingImportContext { Cph = cph };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.RawHoldings.Should().ContainSingle();
        context.RawHoldings[0].LID_FULL_IDENTIFIER.Should().Be(cph);

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

        var context = new CtsHoldingImportContext { Cph = cph };

        // Act
        await _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        context.RawHoldings.Should().BeEmpty();
        context.RawAgents.Should().BeEmpty();
        context.RawKeepers.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenClientThrowsRetryableException_PropagatesException()
    {
        // Arrange
        var context = new CtsHoldingImportContext { Cph = "AH-123456789" };
        var exception = new RetryableException("Transient error");

        _dataBridgeClientMock.Setup(x => x.GetCtsHoldingsAsync(context.Cph, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        _dataBridgeClientMock.Setup(x => x.GetCtsAgentsAsync(context.Cph, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _dataBridgeClientMock.Setup(x => x.GetCtsKeepersAsync(context.Cph, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var act = () => _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RetryableException>().WithMessage("Transient error");
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenClientThrowsNonRetryableException_PropagatesException()
    {
        // Arrange
        var context = new CtsHoldingImportContext { Cph = "AH-123456789" };
        var exception = new NonRetryableException("Permanent error");

        _dataBridgeClientMock.Setup(x => x.GetCtsHoldingsAsync(context.Cph, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _dataBridgeClientMock.Setup(x => x.GetCtsAgentsAsync(context.Cph, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        _dataBridgeClientMock.Setup(x => x.GetCtsKeepersAsync(context.Cph, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var act = () => _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NonRetryableException>().WithMessage("Permanent error");
    }

    [Fact]
    public async Task ExecuteCoreAsync_WhenUnexpectedExceptionOccurs_PropagatesException()
    {
        // Arrange
        var context = new CtsHoldingImportContext { Cph = "AH-123456789" };
        var exception = new ArgumentNullException("Validation failed inside client");

        _dataBridgeClientMock.Setup(x => x.GetCtsHoldingsAsync(context.Cph, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var act = () => _sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'Validation failed inside client')");
    }
}