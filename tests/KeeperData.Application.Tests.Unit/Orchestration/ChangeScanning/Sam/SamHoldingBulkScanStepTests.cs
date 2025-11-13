using FluentAssertions;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using KeeperData.Tests.Common.Factories.UseCases;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Sam;

public class SamHoldingBulkScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<SamHoldingBulkScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5, DelayBetweenQueriesSeconds = 0 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();

    private readonly SamHoldingBulkScanStep _scanStep;
    private readonly SamBulkScanContext _context;

    public SamHoldingBulkScanStepTests()
    {
        _scanStep = new SamHoldingBulkScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            _loggerMock.Object);

        _context = new SamBulkScanContext
        {
            Holders = new(),
            Holdings = new()
        };
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishMessages_AndUpdateContext_WhenDataReturned()
    {
        var responseMock = MockSamData.GetSamHoldingsDataBridgeResponse(
            top: 5,
            count: 5,
            totalCount: 5);

        _dataBridgeClientMock
            .Setup(c => c.GetSamHoldingsAsync(5, 0, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamImportHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
        _context.Holdings.CurrentSkip.Should().Be(5);
        _context.Holdings.ScanCompleted.Should().BeTrue();

        _delayProviderMock.Verify(d => d.DelayAsync(
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldCompleteScan_WhenNoDataReturned()
    {
        var responseMock = MockSamData.GetSamHoldingsDataBridgeResponse(
            top: 5,
            count: 0,
            totalCount: 0);

        _dataBridgeClientMock
            .Setup(c => c.GetSamHoldingsAsync(5, 0, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamImportHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _context.Holdings.ScanCompleted.Should().BeTrue();

        _delayProviderMock.Verify(d => d.DelayAsync(
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldLoopThroughPages_WhenMoreDataAvailable()
    {
        var page1ResponseMock = MockSamData.GetSamHoldingsDataBridgeResponse(
            top: 5,
            count: 5,
            totalCount: 8);
        var page2ResponseMock = MockSamData.GetSamHoldingsDataBridgeResponse(
            top: 5,
            count: 3,
            totalCount: 8);

        _dataBridgeClientMock
            .SetupSequence(c => c.GetSamHoldingsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page1ResponseMock)
            .ReturnsAsync(page2ResponseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamImportHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(8));
        _context.Holdings.CurrentSkip.Should().Be(8);
        _context.Holdings.ScanCompleted.Should().BeTrue();

        _delayProviderMock.Verify(d => d.DelayAsync(
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishDistinctIdentifiersOnly()
    {
        var duplicateId = Guid.NewGuid().ToString();

        var responseMock = new DataBridgeResponse<SamCphHolding>
        {
            CollectionName = "collection",
            Data =
            [
                new() { CPH = duplicateId },
                new() { CPH = duplicateId },
                new() { CPH = Guid.NewGuid().ToString() },
                new() { CPH = Guid.NewGuid().ToString() },
                new() { CPH = Guid.NewGuid().ToString() }
            ],
            Count = 5,
            TotalCount = 5,
            Top = 5,
            Skip = 0
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamHoldingsAsync(5, 0, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamImportHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(4));

        _context.Holdings.CurrentSkip.Should().Be(5);
        _context.Holdings.ScanCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldRespectCancellationToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await _scanStep.ExecuteAsync(_context, cts.Token);

        _dataBridgeClientMock.Verify(c => c.GetSamHoldersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Never);
        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamImportHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenDelaySet_WhenExecuteCoreAsync_ThenShouldLoopThroughPagesWithDelay()
    {
        _config.DelayBetweenQueriesSeconds = 2;

        var page1ResponseMock = MockSamData.GetSamHoldingsDataBridgeResponse(
            top: 5,
            count: 5,
            totalCount: 8);
        var page2ResponseMock = MockSamData.GetSamHoldingsDataBridgeResponse(
            top: 5,
            count: 3,
            totalCount: 8);

        _dataBridgeClientMock
            .SetupSequence(c => c.GetSamHoldingsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page1ResponseMock)
            .ReturnsAsync(page2ResponseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamImportHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(8));
        _context.Holdings.CurrentSkip.Should().Be(8);
        _context.Holdings.ScanCompleted.Should().BeTrue();

        _delayProviderMock.Verify(d => d.DelayAsync(
            TimeSpan.FromSeconds(_config.DelayBetweenQueriesSeconds),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}