using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Daily.Steps;
using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.ApiClients.DataBridgeApi.Configuration;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Providers;
using KeeperData.Tests.Common.Factories.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Orchestration.ChangeScanning.Sam;

public class SamHoldingDailyScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<SamHoldingDailyScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5, DelayBetweenQueriesSeconds = 0 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();
    private readonly IConfiguration _configuration;

    private readonly SamHoldingDailyScanStep _scanStep;
    private readonly SamDailyScanContext _context;

    public SamHoldingDailyScanStepTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamHoldingsEnabled", "true" } })
            .Build();

        _scanStep = new SamHoldingDailyScanStep(
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
            Holdings = new()
        };
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldQueryWithCorrectDateTimeFilter()
    {
        var responseMock = MockSamData.GetSamHoldingsScanIdentifierDataBridgeResponse(0, 0, 0);
        _dataBridgeClientMock
            .Setup(c => c.GetSamHoldingsAsync<SamScanHoldingIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamHoldingsAsync<SamScanHoldingIdentifier>(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.Is<DateTime?>(d => d.HasValue && d.Value.Subtract(_context.UpdatedSinceDateTime!.Value).TotalSeconds < 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishSamUpdateHoldingMessage()
    {
        var responseMock = MockSamData.GetSamHoldingsScanIdentifierDataBridgeResponse(1, 1, 1);
        _dataBridgeClientMock
            .Setup(c => c.GetSamHoldingsAsync<SamScanHoldingIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(It.IsAny<SamUpdateHoldingMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}