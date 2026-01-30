using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using KeeperData.Tests.Common.Factories.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Sam;

public class SamHolderDailyScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<SamHolderDailyScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5, DelayBetweenQueriesSeconds = 0 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();
    private readonly IConfiguration _configuration;

    private readonly SamHolderDailyScanStep _scanStep;
    private readonly SamDailyScanContext _context;

    public SamHolderDailyScanStepTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamHoldersEnabled", "true" } })
            .Build();

        _scanStep = new SamHolderDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            _configuration,
            _loggerMock.Object);

        _context = new SamDailyScanContext
        {
            CurrentDateTime = DateTime.UtcNow,
            UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24),
            Holders = new()
        };
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldExitWhenSamHoldersDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamHoldersEnabled", "false" } })
            .Build();

        var scanStep = new SamHolderDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            configuration,
            _loggerMock.Object);

        await scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamPartiesAsync<SamScanHolderIdentifier>(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldMarkScanCompleted_WhenNoHoldersReturned()
    {
        _dataBridgeClientMock
            .Setup(c => c.GetSamHoldersAsync<SamScanHolderIdentifier>(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataBridgeResponse<SamScanHolderIdentifier> { CollectionName = "collection", Data = [] });

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(_context.Holders.ScanCompleted);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldQueryWithCorrectDateTimeFilter()
    {
        var responseMock = MockSamData.GetSamHolderScanIdentifierDataBridgeResponse(0, 0, 0);
        _dataBridgeClientMock
            .Setup(c => c.GetSamHoldersAsync<SamScanHolderIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamHoldersAsync<SamScanHolderIdentifier>(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.Is<DateTime?>(d => d.HasValue && d.Value.Subtract(_context.UpdatedSinceDateTime!.Value).TotalSeconds < 1),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishSamUpdateHolderMessage()
    {
        var responseMock = MockSamData.GetSamHolderScanIdentifierDataBridgeResponse(1, 1, 1);
        _dataBridgeClientMock
            .Setup(c => c.GetSamHoldersAsync<SamScanHolderIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamUpdateHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenApiThrowsRetryableException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamHoldersEnabled", "true" } })
            .Build();

        var context = new SamDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Holders = new() };

        _dataBridgeClientMock
            .Setup(x => x.GetSamHoldersAsync<SamScanHolderIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("Something went wrong"));

        var scanStep = new SamHolderDailyScanStep(
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
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamHoldersEnabled", "true" } })
            .Build();

        var context = new SamDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Holders = new() };

        _dataBridgeClientMock
            .Setup(x => x.GetSamHoldersAsync<SamScanHolderIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("Something went wrong"));

        var scanStep = new SamHolderDailyScanStep(
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