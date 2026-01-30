using KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Daily.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using KeeperData.Tests.Common.Factories.UseCases;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Cts;

public class CtsKeeperDailyScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<CtsKeeperDailyScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();
    private readonly CtsDailyScanContext _context;
    private readonly CtsKeeperDailyScanStep _scanStep;

    public CtsKeeperDailyScanStepTests()
    {
        _scanStep = new CtsKeeperDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            _loggerMock.Object);

        _context = new CtsDailyScanContext
        {
            CurrentDateTime = DateTime.UtcNow,
            UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24),
            Keepers = new()
        };
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishCtsUpdateKeeperMessage()
    {
        var responseMock = MockCtsData.GetCtsAgentOrKeeperScanIdentifierDataBridgeResponse(1, 1, 1);
        _dataBridgeClientMock
            .Setup(c => c.GetCtsKeepersAsync<CtsScanAgentOrKeeperIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<CtsUpdateKeeperMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldQueryWithCorrectDateTimeFilter()
    {
        var responseMock = MockCtsData.GetCtsAgentOrKeeperScanIdentifierDataBridgeResponse(0, 0, 0);
        _dataBridgeClientMock
            .Setup(c => c.GetCtsKeepersAsync<CtsScanAgentOrKeeperIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetCtsKeepersAsync<CtsScanAgentOrKeeperIdentifier>(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.Is<DateTime?>(d => d.HasValue && d.Value.Subtract(_context.UpdatedSinceDateTime!.Value).TotalSeconds < 1),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenApiThrowsRetryableException()
    {
        // Arrange
        _dataBridgeClientMock
            .Setup(x => x.GetCtsKeepersAsync<CtsScanAgentOrKeeperIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("Something went wrong"));

        // Act
        Func<Task> act = () => _scanStep.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RetryableException>();
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenApiThrowsNonRetryableException()
    {
        // Arrange
        _dataBridgeClientMock
            .Setup(x => x.GetCtsKeepersAsync<CtsScanAgentOrKeeperIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("Something went wrong"));

        // Act
        Func<Task> act = () => _scanStep.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NonRetryableException>();
    }
}