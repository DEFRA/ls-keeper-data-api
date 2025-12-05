using FluentAssertions;
using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Application.Services.BatchCompletion;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.Services.BatchCompletion;

public class BatchCompletionNotificationServiceTests
{
    private readonly Mock<IMessagePublisher<IntakeEventsQueueClient>> _mockQueuePublisher = new();
    private readonly Mock<ILogger<BatchCompletionNotificationService>> _mockLogger = new();
    private readonly BatchCompletionNotificationService _sut;

    public BatchCompletionNotificationServiceTests()
    {
        _sut = new BatchCompletionNotificationService(_mockQueuePublisher.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task NotifyBatchCompletionAsync_WhenCalledWithSamBulkScanContext_PublishesCompletionMessage()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var currentDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var context = new SamBulkScanContext
        {
            ScanCorrelationId = correlationId,
            CurrentDateTime = currentDateTime,
            UpdatedSinceDateTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PageSize = 50,
            Holders = new EntityScanContext { TotalCount = 100, CurrentCount = 10 },
            Holdings = new EntityScanContext { TotalCount = 200, CurrentCount = 20 }
        };

        // Act
        await _sut.NotifyBatchCompletionAsync(context);

        // Assert
        _mockQueuePublisher.Verify(
            x => x.PublishAsync(It.Is<BatchCompletionMessage>(msg =>
                msg.BatchType == Core.Messaging.Contracts.V1.BatchType.SamBulkScan &&
                msg.ScanCorrelationId == correlationId.ToString() &&
                msg.BatchStartTime == currentDateTime &&
                msg.TotalRecordsProcessed == 300 &&
                msg.MessagesPublished == 30 &&
                msg.Metadata != null &&
                msg.Metadata.ContainsKey("HolderRecordsProcessed") &&
                msg.Metadata["HolderRecordsProcessed"].Equals(100) &&
                msg.Metadata.ContainsKey("HoldingRecordsProcessed") &&
                msg.Metadata["HoldingRecordsProcessed"].Equals(200) &&
                msg.Metadata.ContainsKey("PageSize") &&
                msg.Metadata["PageSize"].Equals(50)
            ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyBatchCompletionAsync_WhenCalledWithUnsupportedContext_DoesNotPublishMessage()
    {
        // Arrange
        var unsupportedContext = new { SomeProperty = "value" };

        // Act
        await _sut.NotifyBatchCompletionAsync(unsupportedContext);

        // Assert
        _mockQueuePublisher.Verify(
            x => x.PublishAsync(It.IsAny<BatchCompletionMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task NotifyBatchCompletionAsync_WhenPublishingFails_LogsErrorAndDoesNotThrow()
    {
        // Arrange
        var context = new SamBulkScanContext
        {
            ScanCorrelationId = Guid.NewGuid(),
            CurrentDateTime = DateTime.UtcNow,
            Holders = new EntityScanContext(),
            Holdings = new EntityScanContext()
        };

        _mockQueuePublisher.Setup(x => x.PublishAsync(It.IsAny<BatchCompletionMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var act = () => _sut.NotifyBatchCompletionAsync(context);

        // Assert
        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish batch completion notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyBatchCompletionAsync_WhenCalledWithCtsBulkScanContext_PublishesCompletionMessage()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        var currentDateTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var context = new CtsBulkScanContext
        {
            ScanCorrelationId = correlationId,
            CurrentDateTime = currentDateTime,
            UpdatedSinceDateTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PageSize = 75,
            Holdings = new EntityScanContext { TotalCount = 150, CurrentCount = 15 }
        };

        // Act
        await _sut.NotifyBatchCompletionAsync(context);

        // Assert
        _mockQueuePublisher.Verify(
            x => x.PublishAsync(It.Is<BatchCompletionMessage>(msg =>
                msg.BatchType == Core.Messaging.Contracts.V1.BatchType.CtsBulkScan &&
                msg.ScanCorrelationId == correlationId.ToString() &&
                msg.BatchStartTime == currentDateTime &&
                msg.TotalRecordsProcessed == 150 &&
                msg.MessagesPublished == 15 &&
                msg.Metadata != null &&
                msg.Metadata.ContainsKey("HoldingRecordsProcessed") &&
                msg.Metadata["HoldingRecordsProcessed"].Equals(150) &&
                msg.Metadata.ContainsKey("PageSize") &&
                msg.Metadata["PageSize"].Equals(75)
            ), It.IsAny<CancellationToken>()), Times.Once);
    }
}