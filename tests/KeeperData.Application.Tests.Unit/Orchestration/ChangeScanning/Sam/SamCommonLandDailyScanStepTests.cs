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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Sam;

public class SamCommonLandDailyScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<SamCommonLandDailyScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5, DelayBetweenQueriesSeconds = 0 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();
    private readonly IConfiguration _configuration;

    private readonly SamCommonLandDailyScanStep _scanStep;
    private readonly SamDailyScanContext _context;

    public SamCommonLandDailyScanStepTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamCommonLandsEnabled", "true" } })
            .Build();

        _scanStep = new SamCommonLandDailyScanStep(
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
            CommonLands = new()
        };
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldExitWhenSamCommonLandsDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamCommonLandsEnabled", "false" } })
            .Build();

        var scanStep = new SamCommonLandDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            configuration,
            _loggerMock.Object);

        await scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldMarkScanCompleted_WhenNoCommonLandsReturned()
    {
        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataBridgeResponse<SamScanCommonLandIdentifier> { CollectionName = "collection", Data = [] });

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(_context.CommonLands.ScanCompleted);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldQueryWithCorrectDateTimeFilter()
    {
        var responseMock = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Top = 0,
            Skip = 0,
            Count = 0,
            TotalCount = 0,
            Data = []
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.Is<DateTime?>(d => d.HasValue && d.Value.Subtract(_context.UpdatedSinceDateTime!.Value).TotalSeconds < 1),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishSamUpdateHoldingMessage()
    {
        var responseMock = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Top = 1,
            Skip = 0,
            Count = 1,
            TotalCount = 1,
            Data = [new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0001" }]
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamUpdateHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishDistinctMessages_WhenDuplicateCommonCphsReturned()
    {
        var responseMock = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Top = 3,
            Skip = 0,
            Count = 3,
            TotalCount = 3,
            Data =
            [
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0001" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0001" }, // Duplicate
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0002" }
            ]
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        // Should only publish 2 messages (distinct CPHs)
        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamUpdateHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldSkipNullOrEmptyCommonCphs()
    {
        var responseMock = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Top = 4,
            Skip = 0,
            Count = 4,
            TotalCount = 4,
            Data =
            [
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0001" },
                new SamScanCommonLandIdentifier { COMMON_CPH = null },
                new SamScanCommonLandIdentifier { COMMON_CPH = "" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0002" }
            ]
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        // Should only publish 2 messages (valid CPHs)
        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamUpdateHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishMessageWithCorrectCommonCph()
    {
        var expectedCph = "12/345/6789";
        var responseMock = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Top = 1,
            Skip = 0,
            Count = 1,
            TotalCount = 1,
            Data = [new SamScanCommonLandIdentifier { COMMON_CPH = expectedCph }]
        };

        SamUpdateHoldingMessage? capturedMessage = null;

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        _messagePublisherMock
            .Setup(p => p.PublishAsync(It.IsAny<SamUpdateHoldingMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SamUpdateHoldingMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        capturedMessage.Should().NotBeNull();
        capturedMessage!.Identifier.Should().Be(expectedCph);
        capturedMessage.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldUseCorrectSelectFieldsAndOrderBy()
    {
        var responseMock = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Data = []
        };

        string? capturedSelectFields = null;
        string? capturedOrderBy = null;

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<int, int, string, DateTime?, string, CancellationToken>((_, _, select, _, order, _) =>
            {
                capturedSelectFields = select;
                capturedOrderBy = order;
            })
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        capturedSelectFields.Should().Be("COMMON_CPH");
        capturedOrderBy.Should().Be("COMMON_CPH asc");
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldBubbleException_WhenApiThrowsRetryableException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamCommonLandsEnabled", "true" } })
            .Build();

        var context = new SamDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), CommonLands = new() };

        _dataBridgeClientMock
            .Setup(x => x.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("Something went wrong"));

        var scanStep = new SamCommonLandDailyScanStep(
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
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamCommonLandsEnabled", "true" } })
            .Build();

        var context = new SamDailyScanContext { CurrentDateTime = DateTime.UtcNow, UpdatedSinceDateTime = DateTime.UtcNow.AddHours(-24), CommonLands = new() };

        _dataBridgeClientMock
            .Setup(x => x.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("Something went wrong"));

        var scanStep = new SamCommonLandDailyScanStep(
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
    public async Task ExecuteCoreAsync_ShouldIncrementCountsCorrectly()
    {
        var responseMock = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Top = 5,
            Skip = 0,
            Count = 3,
            TotalCount = 3,
            Data =
            [
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0001" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0002" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0003" }
            ]
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _context.CommonLands.TotalCount.Should().Be(3);
        _context.CommonLands.CurrentCount.Should().Be(3);
        _context.CommonLands.ScanCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldHandleMultiplePagesCorrectly()
    {
        // First page - 5 items (full page)
        var firstPageResponse = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Top = 5,
            Skip = 0,
            Count = 5,
            TotalCount = 7,
            Data =
            [
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0001" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0002" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0003" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0004" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0005" }
            ]
        };

        // Second page - 2 items (partial page, triggers completion)
        var secondPageResponse = new DataBridgeResponse<SamScanCommonLandIdentifier>
        {
            CollectionName = "collection",
            Top = 5,
            Skip = 5,
            Count = 2,
            TotalCount = 7,
            Data =
            [
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0006" },
                new SamScanCommonLandIdentifier { COMMON_CPH = "00/000/0007" }
            ]
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstPageResponse);

        _dataBridgeClientMock
            .Setup(c => c.GetSamCommonLandsAsync<SamScanCommonLandIdentifier>(5, 5, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondPageResponse);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _context.CommonLands.TotalCount.Should().Be(7);
        _context.CommonLands.CurrentCount.Should().Be(2); // CurrentCount is the last batch count
        _context.CommonLands.CurrentSkip.Should().Be(7); // Total processed
        _context.CommonLands.ScanCompleted.Should().BeTrue();
        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamUpdateHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(7));
    }
}