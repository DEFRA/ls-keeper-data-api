using FluentAssertions;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Cts;

public class CtsAgentDailyScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<CtsAgentDailyScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishCtsUpdateAgentMessage_WhenFlagIsEnabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:CtsAgentsEnabled", "true" } })
            .Build();

        var context = new CtsDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Agents = new() };
        var responseMock = MockCtsData.GetCtsAgentOrKeeperScanIdentifierDataBridgeResponse(1, 1, 1);

        _dataBridgeClientMock
            .Setup(c => c.GetCtsAgentsAsync<CtsScanAgentOrKeeperIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        var scanStep = new CtsAgentDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            config,
            _loggerMock.Object);

        await scanStep.ExecuteAsync(context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<CtsUpdateAgentMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldNotPublish_WhenFlagIsDisabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:CtsAgentsEnabled", "false" } })
            .Build();

        var context = new CtsDailyScanContext { CurrentDateTime = DateTime.UtcNow, Agents = new() };

        var scanStep = new CtsAgentDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            config,
            _loggerMock.Object);

        await scanStep.ExecuteAsync(context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<CtsUpdateAgentMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldQueryWithCorrectDateTimeFilter_WhenFlagIsEnabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:CtsAgentsEnabled", "true" } })
            .Build();

        var context = new CtsDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Agents = new() };
        var responseMock = MockCtsData.GetCtsAgentOrKeeperScanIdentifierDataBridgeResponse(0, 0, 0);
        _dataBridgeClientMock
            .Setup(c => c.GetCtsAgentsAsync<CtsScanAgentOrKeeperIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        var scanStep = new CtsAgentDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            config,
            _loggerMock.Object);

        await scanStep.ExecuteAsync(context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetCtsAgentsAsync<CtsScanAgentOrKeeperIdentifier>(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.Is<DateTime?>(d => d.HasValue && d.Value.Subtract(context.UpdatedSinceDateTime!.Value).TotalSeconds < 1),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenApiThrowsRetryableException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:CtsAgentsEnabled", "true" } })
            .Build();

        var context = new CtsDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Agents = new() };

        _dataBridgeClientMock
            .Setup(x => x.GetCtsAgentsAsync<CtsScanAgentOrKeeperIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("Something went wrong"));

        var scanStep = new CtsAgentDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            config,
            _loggerMock.Object);

        // Act
        Func<Task> act = () => scanStep.ExecuteAsync(context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RetryableException>();
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenApiThrowsNonRetryableException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:CtsAgentsEnabled", "true" } })
            .Build();

        var context = new CtsDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Agents = new() };

        _dataBridgeClientMock
            .Setup(x => x.GetCtsAgentsAsync<CtsScanAgentOrKeeperIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("Something went wrong"));

        var scanStep = new CtsAgentDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            config,
            _loggerMock.Object);

        // Act
        Func<Task> act = () => scanStep.ExecuteAsync(context, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NonRetryableException>();
    }
}