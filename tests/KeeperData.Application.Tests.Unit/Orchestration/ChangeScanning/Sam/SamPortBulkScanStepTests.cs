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
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Sam;

public class SamPortBulkScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<SamPortBulkScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5, DelayBetweenQueriesSeconds = 0 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();

    private readonly SamPortBulkScanStep _scanStep;
    private readonly SamBulkScanContext _context;

    public SamPortBulkScanStepTests()
    {
        _scanStep = new SamPortBulkScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            _loggerMock.Object);

        _context = new SamBulkScanContext
        {
            Holdings = new(),
            Ports = new()
        };
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPublishMessages_AndUpdateContext_WhenDataReturned()
    {
        var responseMock = new DataBridgeResponse<SamScanPortIdentifier>
        {
            CollectionName = "collection",
            Count = 2,
            TotalCount = 2,
            Top = 5,
            Skip = 0,
            Data = [
                new SamScanPortIdentifier { CPH = "12/345/6789" },
                new SamScanPortIdentifier { CPH = "98/765/4321" }
            ]
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamPortsAsync<SamScanPortIdentifier>(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamImportHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _context.Ports.CurrentSkip.Should().Be(2);
        _context.Ports.ScanCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteScan_WhenNoDataReturned()
    {
        var responseMock = new DataBridgeResponse<SamScanPortIdentifier>
        {
            CollectionName = "collection",
            Count = 0,
            TotalCount = 0,
            Top = 5,
            Skip = 0,
            Data = []
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamPortsAsync<SamScanPortIdentifier>(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamImportHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _context.Ports.ScanCompleted.Should().BeTrue();
    }
}