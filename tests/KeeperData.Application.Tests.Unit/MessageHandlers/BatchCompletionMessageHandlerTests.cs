using FluentAssertions;
using KeeperData.Application.MessageHandlers;
using KeeperData.Core.Messaging;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Application.Tests.Unit.MessageHandlers;

public class BatchCompletionMessageHandlerTests
{
    private readonly Mock<IUnwrappedMessageSerializer<BatchCompletionMessage>> _mockSerializer = new();
    private readonly Mock<IMessagePublisher<BatchCompletionTopicClient>> _mockTopicPublisher = new();
    private readonly Mock<ILogger<BatchCompletionMessageHandler>> _mockLogger = new();
    private readonly BatchCompletionMessageHandler _sut;

    public BatchCompletionMessageHandlerTests()
    {
        _sut = new BatchCompletionMessageHandler(_mockSerializer.Object, _mockTopicPublisher.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenCalledWithValidMessage_DeserializesAndPublishesToSNS()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var unwrappedMessage = new UnwrappedMessage
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            Payload = "test payload"
        };

        var batchCompletionMessage = new BatchCompletionMessage
        {
            BatchType = BatchType.SamBulkScan,
            ScanCorrelationId = correlationId,
            TotalRecordsProcessed = 100,
            MessagesPublished = 10,
            BatchStartTime = DateTime.UtcNow.AddMinutes(-5),
            BatchCompletionTime = DateTime.UtcNow
        };

        _mockSerializer.Setup(x => x.Deserialize(unwrappedMessage))
            .Returns(batchCompletionMessage);

        // Act
        var result = await _sut.Handle(unwrappedMessage);

        // Assert
        result.Should().Be(batchCompletionMessage);

        _mockTopicPublisher.Verify(
            x => x.PublishAsync(batchCompletionMessage, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMessageIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        UnwrappedMessage? nullMessage = null;

        // Act & Assert
        var act = () => _sut.Handle(nullMessage!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WhenDeserializationReturnsNull_ThrowsNonRetryableException()
    {
        // Arrange
        var unwrappedMessage = new UnwrappedMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = "invalid payload"
        };

        _mockSerializer.Setup(x => x.Deserialize(unwrappedMessage))
            .Returns((BatchCompletionMessage?)null);

        // Act & Assert
        var act = () => _sut.Handle(unwrappedMessage);
        await act.Should().ThrowAsync<NonRetryableException>();
    }

    [Fact]
    public async Task Handle_WhenSNSPublishingFails_ContinuesProcessingAndLogsError()
    {
        // Arrange
        var unwrappedMessage = new UnwrappedMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = Guid.NewGuid().ToString(),
            Payload = "test payload"
        };

        var batchCompletionMessage = new BatchCompletionMessage
        {
            BatchType = BatchType.CtsBulkScan,
            ScanCorrelationId = Guid.NewGuid().ToString()
        };

        _mockSerializer.Setup(x => x.Deserialize(unwrappedMessage))
            .Returns(batchCompletionMessage);

        _mockTopicPublisher.Setup(x => x.PublishAsync(It.IsAny<BatchCompletionMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SNS publish failed"));

        // Act
        var result = await _sut.Handle(unwrappedMessage);

        // Assert
        result.Should().Be(batchCompletionMessage);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish batch completion to SNS topic")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}