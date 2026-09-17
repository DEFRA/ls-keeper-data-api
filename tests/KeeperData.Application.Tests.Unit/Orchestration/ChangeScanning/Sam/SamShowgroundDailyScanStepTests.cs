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

public class SamShowgroundDailyScanStepTests
{
    private readonly Mock<IDataBridgeClient> _dataBridgeClientMock = new();
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _messagePublisherMock = new();
    private readonly Mock<ILogger<SamShowgroundDailyScanStep>> _loggerMock = new();
    private readonly DataBridgeScanConfiguration _config = new() { QueryPageSize = 5, DelayBetweenQueriesSeconds = 0 };
    private readonly Mock<IDelayProvider> _delayProviderMock = new();
    private readonly IConfiguration _configuration;

    private readonly SamShowgroundDailyScanStep _scanStep;
    private readonly SamDailyScanContext _context;

    public SamShowgroundDailyScanStepTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamShowgroundsEnabled", "true" } })
            .Build();

        _scanStep = new SamShowgroundDailyScanStep(
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
            Showgrounds = new()
        };
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldExitWhenSamShowgroundsDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "DataBridgeCollectionFlags:SamShowgroundsEnabled", "false" } })
            .Build();

        var scanStep = new SamShowgroundDailyScanStep(
            _dataBridgeClientMock.Object,
            _messagePublisherMock.Object,
            _config,
            _delayProviderMock.Object,
            configuration,
            _loggerMock.Object);

        await scanStep.ExecuteAsync(_context, CancellationToken.None);

        _dataBridgeClientMock.Verify(c => c.GetSamShowgroundsAsync<SamScanShowgroundIdentifier>(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteCoreAsync_ShouldPublishSamUpdateHoldingMessage()
    {
        var expectedCph = "12/345/6789";
        var responseMock = new DataBridgeResponse<SamScanShowgroundIdentifier>
        {
            CollectionName = "collection",
            Top = 1,
            Skip = 0,
            Count = 1,
            TotalCount = 1,
            Data = [new SamScanShowgroundIdentifier { CPH = expectedCph }]
        };

        _dataBridgeClientMock
            .Setup(c => c.GetSamShowgroundsAsync<SamScanShowgroundIdentifier>(5, 0, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMock);

        await _scanStep.ExecuteAsync(_context, CancellationToken.None);

        _messagePublisherMock.Verify(p => p.PublishAsync(
            It.Is<SamUpdateHoldingMessage>(m => m.Identifier == expectedCph),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}