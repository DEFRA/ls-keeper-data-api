using FluentAssertions;
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

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Sam;

public class SamPartyDailyScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<SamPartyDailyScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5, DelayBetweenQueriesSeconds = 0 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();
    private readonly IConfiguration _configuration;

    private readonly SamPartyDailyScanStep _scanStep;
    private readonly SamDailyScanContext _context;

    public SamPartyDailyScanStepTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamPartiesEnabled", "true" } })
            .Build();

        _scanStep = new SamPartyDailyScanStep(
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
            Parties = new()
        };
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldExitWhenSamPartiesDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamPartiesEnabled", "false" } })
            .Build();

        var scanStep = new SamPartyDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            configuration,
            _loggerMock.Object);

        await scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamPartiesAsync<SamScanPartyIdentifier>(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldMarkScanCompleted_WhenNoPartiesReturned()
    {
        _dataBridgeClientMock
            .Setup(c => c.GetSamPartiesAsync<SamScanPartyIdentifier>(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataBridgeResponse<SamScanPartyIdentifier> { CollectionName = "collection", Data = [] });

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(_context.Parties.ScanCompleted);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldQueryWithCorrectDateTimeFilter()
    {
        var partiesResponseMock = MockSamData.GetSamPartiesScanIdentifierDataBridgeResponse(0, 0, 0);
        var herdsResponseMock = MockSamData.GetSamHerdsScanIdentifierDataBridgeResponse(1, 1, 1);

        _dataBridgeClientMock
            .Setup(c => c.GetSamPartiesAsync<SamScanPartyIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partiesResponseMock);

        _dataBridgeClientMock
            .Setup(c => c.GetSamHerdsByPartyIdAsync<SamScanHerdIdentifier>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(herdsResponseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamPartiesAsync<SamScanPartyIdentifier>(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.Is<DateTime?>(d => d.HasValue && d.Value.Subtract(_context.UpdatedSinceDateTime!.Value).TotalSeconds < 1),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _dataBridgeClientMock.Verify(c => c.GetSamHerdsByPartyIdAsync<SamScanHerdIdentifier>(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishSamUpdatePartyMessage()
    {
        var partiesResponseMock = MockSamData.GetSamPartiesScanIdentifierDataBridgeResponse(5, 5, 5);
        var herdsResponseMock = MockSamData.GetSamHerdsScanIdentifierDataBridgeResponse(1, 1, 1);

        _dataBridgeClientMock
            .Setup(c => c.GetSamPartiesAsync<SamScanPartyIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partiesResponseMock);

        _dataBridgeClientMock
            .Setup(c => c.GetSamHerdsByPartyIdAsync<SamScanHerdIdentifier>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(herdsResponseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamHerdsByPartyIdAsync<SamScanHerdIdentifier>(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Exactly(5));

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamUpdateHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenGetSamPartiesAsyncThrowsRetryableException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamPartiesEnabled", "true" } })
            .Build();

        var context = new SamDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Parties = new() };

        _dataBridgeClientMock
            .Setup(x => x.GetSamPartiesAsync<SamScanPartyIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("Something went wrong"));

        var scanStep = new SamPartyDailyScanStep(
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
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenGetSamPartiesAsyncThrowsNonRetryableException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamPartiesEnabled", "true" } })
            .Build();

        var context = new SamDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Parties = new() };

        _dataBridgeClientMock
            .Setup(x => x.GetSamPartiesAsync<SamScanPartyIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("Something went wrong"));

        var scanStep = new SamPartyDailyScanStep(
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

    [Fact]
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenGetSamHerdsByPartyIdAsyncThrowsRetryableException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamPartiesEnabled", "true" } })
            .Build();

        var context = new SamDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Parties = new() };

        var partiesResponseMock = MockSamData.GetSamPartiesScanIdentifierDataBridgeResponse(1, 1, 1);
        _dataBridgeClientMock
            .Setup(c => c.GetSamPartiesAsync<SamScanPartyIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partiesResponseMock);

        _dataBridgeClientMock
            .Setup(x => x.GetSamHerdsByPartyIdAsync<SamScanHerdIdentifier>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("Something went wrong"));

        var scanStep = new SamPartyDailyScanStep(
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
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenGetSamHerdsByPartyIdAsyncThrowsNonRetryableException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamPartiesEnabled", "true" } })
            .Build();

        var context = new SamDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), Parties = new() };

        var partiesResponseMock = MockSamData.GetSamPartiesScanIdentifierDataBridgeResponse(1, 1, 1);
        _dataBridgeClientMock
            .Setup(c => c.GetSamPartiesAsync<SamScanPartyIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partiesResponseMock);

        _dataBridgeClientMock
            .Setup(x => x.GetSamHerdsByPartyIdAsync<SamScanHerdIdentifier>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("Something went wrong"));

        var scanStep = new SamPartyDailyScanStep(
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