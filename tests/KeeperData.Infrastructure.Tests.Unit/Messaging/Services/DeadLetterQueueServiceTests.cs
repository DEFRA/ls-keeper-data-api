using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Core.DeadLetter;
using KeeperData.Core.Exceptions;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Services;

public class DeadLetterQueueServiceTests
{
    private static readonly List<string> QueueStatsAttributes = new()
    {
        "ApproximateNumberOfMessages",
        "ApproximateNumberOfMessagesNotVisible",
        "ApproximateNumberOfMessagesDelayed"
    };

    private static readonly List<string> ApproximateMessagesAttribute = new()
    {
        "ApproximateNumberOfMessages"
    };

    private static readonly List<string> AllAttributesList = new() { "All" };

    private readonly Mock<IAmazonSQS> _mockSqs;
    private readonly Mock<ILogger<DeadLetterQueueService>> _mockLogger;
    private readonly IntakeEventQueueOptions _options;
    private readonly string _queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789/main-queue";
    private readonly string _dlqUrl = "https://sqs.us-east-1.amazonaws.com/123456789/main-queue-dlq";

    public DeadLetterQueueServiceTests()
    {
        _mockSqs = new Mock<IAmazonSQS>();
        _mockLogger = new Mock<ILogger<DeadLetterQueueService>>();
        _options = new IntakeEventQueueOptions
        {
            QueueUrl = _queueUrl,
            DeadLetterQueueUrl = _dlqUrl
        };
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_Success_SendsToDlqAndDeletesFromMainQueue()
    {
        // Arrange
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Test failure");

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = "dlq-msg-123" });

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            _queueUrl,
            message.ReceiptHandle,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _mockSqs.Verify(x => x.ChangeMessageVisibilityAsync(
            It.Is<ChangeMessageVisibilityRequest>(r =>
                r.QueueUrl == _queueUrl &&
                r.ReceiptHandle == message.ReceiptHandle &&
                r.VisibilityTimeout == 300),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockSqs.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r =>
                r.QueueUrl == _dlqUrl &&
                r.MessageBody == message.Body &&
                r.MessageAttributes.ContainsKey("DLQ_FailureReason") &&
                r.MessageAttributes["DLQ_FailureReason"].StringValue == "NonRetryableException"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockSqs.Verify(x => x.DeleteMessageAsync(
            _queueUrl,
            message.ReceiptHandle,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_SendFails_DoesNotDeleteFromMainQueue()
    {
        // Arrange
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Test failure");

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("DLQ not found"));

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _mockSqs.Verify(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send to DLQ")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_SendSucceedsButDeleteFails_LogsCriticalError()
    {
        // Arrange - This is the WORST case: duplicate in both queues
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Test failure");

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = "dlq-msg-123" });

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            _queueUrl,
            message.ReceiptHandle,
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Network timeout"));

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("CRITICAL") &&
                    v.ToString()!.Contains("DUPLICATE") &&
                    v.ToString()!.Contains("SendSucceeded: True") &&
                    v.ToString()!.Contains("DeleteSucceeded: False")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_NoDlqConfigured_ReturnsFalseWithoutCalling()
    {
        // Arrange
        var optionsWithoutDlq = new IntakeEventQueueOptions
        {
            QueueUrl = _queueUrl,
            DeadLetterQueueUrl = null
        };
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Test failure");

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(optionsWithoutDlq), _mockLogger.Object);

        // Act
        var result = await sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _mockSqs.Verify(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockSqs.Verify(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_AddsFailureMetadata()
    {
        // Arrange
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Invalid data format");
        SendMessageRequest? capturedRequest = null;

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new SendMessageResponse { MessageId = "dlq-msg-123" });

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        await sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.MessageAttributes.Should().ContainKey("DLQ_FailureReason");
        capturedRequest.MessageAttributes["DLQ_FailureReason"].StringValue.Should().Be("NonRetryableException");

        capturedRequest.MessageAttributes.Should().ContainKey("DLQ_FailureMessage");
        capturedRequest.MessageAttributes["DLQ_FailureMessage"].StringValue.Should().Be("Invalid data format");

        capturedRequest.MessageAttributes.Should().ContainKey("DLQ_OriginalMessageId");
        capturedRequest.MessageAttributes["DLQ_OriginalMessageId"].StringValue.Should().Be(message.MessageId);

        capturedRequest.MessageAttributes.Should().ContainKey("DLQ_ReceiveCount");
        capturedRequest.MessageAttributes["DLQ_ReceiveCount"].StringValue.Should().Be("2");

        capturedRequest.MessageAttributes.Should().ContainKey("DLQ_FailureTimestamp");
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_PreservesOriginalMessageAttributes()
    {
        // Arrange
        var message = CreateTestMessage();
        message.MessageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["CustomerId"] = new MessageAttributeValue { StringValue = "12345", DataType = "String" },
            ["Priority"] = new MessageAttributeValue { StringValue = "High", DataType = "String" }
        };
        var exception = new NonRetryableException("Test failure");
        SendMessageRequest? capturedRequest = null;

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new SendMessageResponse { MessageId = "dlq-msg-123" });

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        await sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.MessageAttributes.Should().ContainKey("CustomerId");
        capturedRequest.MessageAttributes["CustomerId"].StringValue.Should().Be("12345");
        capturedRequest.MessageAttributes.Should().ContainKey("Priority");
        capturedRequest.MessageAttributes["Priority"].StringValue.Should().Be("High");
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_ChangeVisibilityFails_StillAttemptsMove()
    {
        // Arrange
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Test failure");

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Message no longer exists"));

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = "dlq-msg-123" });

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _mockSqs.Verify(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_TruncatesLongExceptionMessage()
    {
        // Arrange
        var message = CreateTestMessage();
        var longMessage = new string('x', 500);
        var exception = new NonRetryableException(longMessage);
        SendMessageRequest? capturedRequest = null;

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new SendMessageResponse { MessageId = "dlq-msg-123" });

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        await sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        var failureMessage = capturedRequest!.MessageAttributes["DLQ_FailureMessage"].StringValue;
        failureMessage.Length.Should().BeLessOrEqualTo(256, $"Expected <= 256 but was {failureMessage.Length}");
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Test failure");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.Is<CancellationToken>(ct => ct.IsCancellationRequested)))
            .ThrowsAsync(new OperationCanceledException());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.MoveToDeadLetterQueueAsync(message, _queueUrl, exception, cts.Token));
    }

    [Fact]
    public async Task GetQueueStatsAsync_ReturnsCorrectStats()
    {
        // Arrange
        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.Is<List<string>>(attrs =>
                attrs.Contains("ApproximateNumberOfMessages") &&
                attrs.Contains("ApproximateNumberOfMessagesNotVisible") &&
                attrs.Contains("ApproximateNumberOfMessagesDelayed")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "5",
                    ["ApproximateNumberOfMessagesNotVisible"] = "2",
                    ["ApproximateNumberOfMessagesDelayed"] = "1"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.GetQueueStatsAsync(_dlqUrl);

        // Assert
        result.Should().NotBeNull();
        result.QueueUrl.Should().Be(_dlqUrl);
        result.ApproximateMessageCount.Should().Be(5);
        result.ApproximateMessagesNotVisible.Should().Be(2);
        result.ApproximateMessagesDelayed.Should().Be(1);
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetQueueStatsAsync_WithEmptyQueue_ReturnsZeroCounts()
    {
        // Arrange
        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0",
                    ["ApproximateNumberOfMessagesNotVisible"] = "0",
                    ["ApproximateNumberOfMessagesDelayed"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.GetQueueStatsAsync(_dlqUrl);

        // Assert
        result.ApproximateMessageCount.Should().Be(0);
        result.ApproximateMessagesNotVisible.Should().Be(0);
        result.ApproximateMessagesDelayed.Should().Be(0);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_ReturnsMessages()
    {
        // Arrange
        var messages = new List<Message>
        {
            CreateTestMessage("msg-1", "correlation-1"),
            CreateTestMessage("msg-2", "correlation-2")
        };

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.Is<ReceiveMessageRequest>(r =>
                r.QueueUrl == _dlqUrl &&
                r.MaxNumberOfMessages == 2 &&
                r.VisibilityTimeout == 30),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = messages });

        // Mock the visibility restoration calls
        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.Is<ChangeMessageVisibilityRequest>(r =>
                r.QueueUrl == _dlqUrl &&
                r.VisibilityTimeout == 0 &&
                (r.ReceiptHandle == messages[0].ReceiptHandle || r.ReceiptHandle == messages[1].ReceiptHandle)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.Is<List<string>>(attrs => attrs.Contains("ApproximateNumberOfMessages")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "10"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(2);

        // Assert
        result.Should().NotBeNull();
        result.Messages.Should().HaveCount(2);
        result.Messages.Should().Contain(m => m.MessageId == "msg-1" && m.CorrelationId == "correlation-1");
        result.Messages.Should().Contain(m => m.MessageId == "msg-2" && m.CorrelationId == "correlation-2");
        result.TotalApproximateCount.Should().Be(10);
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify visibility was restored for both messages
        _mockSqs.Verify(x => x.ChangeMessageVisibilityAsync(
            It.Is<ChangeMessageVisibilityRequest>(r =>
                r.QueueUrl == _dlqUrl &&
                r.VisibilityTimeout == 0),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_ClampsMaxMessagesToTen()
    {
        // Arrange
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.Is<ReceiveMessageRequest>(r => r.MaxNumberOfMessages == 10),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        await sut.PeekDeadLetterMessagesAsync(15);

        // Assert
        _mockSqs.Verify(x => x.ReceiveMessageAsync(
            It.Is<ReceiveMessageRequest>(r => r.MaxNumberOfMessages == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WithNoMessages_ReturnsEmptyList()
    {
        // Arrange
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(5);

        // Assert
        result.Messages.Should().BeEmpty();
        result.TotalApproximateCount.Should().Be(0);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_ParsesDlqMetadata()
    {
        // Arrange
        var message = CreateTestMessage("msg-1", "correlation-1");
        message.MessageAttributes["DLQ_OriginalMessageId"] = new MessageAttributeValue { StringValue = "original-msg-1", DataType = "String" };
        message.MessageAttributes["DLQ_FailureReason"] = new MessageAttributeValue { StringValue = "NonRetryableException", DataType = "String" };
        message.MessageAttributes["DLQ_FailureMessage"] = new MessageAttributeValue { StringValue = "Data format error", DataType = "String" };
        message.MessageAttributes["DLQ_FailureTimestamp"] = new MessageAttributeValue { StringValue = "2024-01-15T10:30:00Z", DataType = "String" };
        message.MessageAttributes["DLQ_ReceiveCount"] = new MessageAttributeValue { StringValue = "3", DataType = "Number" };
        message.MessageAttributes["Subject"] = new MessageAttributeValue { StringValue = "OrderCreated", DataType = "String" };

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "1"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(1);

        // Assert
        var dto = result.Messages.First();
        dto.OriginalMessageId.Should().Be("original-msg-1");
        dto.FailureReason.Should().Be("NonRetryableException");
        dto.FailureMessage.Should().Be("Data format error");
        dto.FailureTimestamp.Should().Be("2024-01-15T10:30:00Z");
        dto.ReceiveCount.Should().Be("3");
        dto.MessageType.Should().Be("OrderCreated");
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WhenMaxMessagesIsZero_RetrievesAllMessagesUpToConfiguredMax()
    {
        // Arrange
        var messages = Enumerable.Range(1, 10)
            .Select(i => CreateTestMessage($"msg-{i}", $"correlation-{i}"))
            .ToList();

        // Mock GetQueueAttributesAsync to return 150 messages in queue
        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.Is<List<string>>(attrs => attrs.Contains("ApproximateNumberOfMessages")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "150"
                }
            });

        // Mock ReceiveMessageAsync to return messages in batches
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = messages });

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(0);

        // Assert
        result.Should().NotBeNull();
        result.TotalApproximateCount.Should().Be(150);
        result.Messages.Should().HaveCount(10); // Got messages back

        // Verify it requested at most the configured maximum (100)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Peeking all") &&
                    v.ToString()!.Contains("150") &&
                    v.ToString()!.Contains("max allowed: 100")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WhenMaxMessagesExceedsConfigured_ClampsToConfiguredMax()
    {
        // Arrange
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        await sut.PeekDeadLetterMessagesAsync(500);

        // Assert - Should be clamped to 100 (default MaxPeekMessages)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Requested 500 messages") &&
                    v.ToString()!.Contains("capped to configured maximum of 100")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WithCustomMaxPeekMessages_RespectsConfiguration()
    {
        // Arrange
        var customOptions = new IntakeEventQueueOptions
        {
            QueueUrl = _queueUrl,
            DeadLetterQueueUrl = _dlqUrl,
            MaxPeekMessages = 50 // Custom max
        };

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "200"
                }
            });

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(customOptions), _mockLogger.Object);

        // Act
        await sut.PeekDeadLetterMessagesAsync(0);

        // Assert - Should be clamped to 50
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("max allowed: 50")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WhenQueueIsEmpty_ReturnsEmptyResult()
    {
        // Arrange
        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(0);

        // Assert
        result.Should().NotBeNull();
        result.Messages.Should().BeEmpty();
        result.TotalApproximateCount.Should().Be(0);
        result.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

        // Should not attempt to receive messages
        _mockSqs.Verify(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WhenReceiveFails_RestoresVisibilityAndThrows()
    {
        // Arrange
        var messages = new List<Message> { CreateTestMessage("msg-1", "correlation-1") };

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("SQS timeout"));

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "10"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AmazonSQSException>(() => sut.PeekDeadLetterMessagesAsync(5));

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error peeking DLQ messages")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WithDuplicateMessageIds_OnlyIncludesOnce()
    {
        // Arrange - Simulate SQS returning duplicate messageIds (shouldn't happen but defensive)
        var message1 = CreateTestMessage("msg-duplicate", "correlation-1");
        var message2 = CreateTestMessage("msg-duplicate", "correlation-2"); // Same messageId

        var receiveCallCount = 0;
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                receiveCallCount++;
                return receiveCallCount == 1
                    ? new ReceiveMessageResponse { Messages = new List<Message> { message1, message2 } }
                    : new ReceiveMessageResponse { Messages = new List<Message>() };
            });

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "2"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(10);

        // Assert
        result.Messages.Should().HaveCount(1); // Only one unique message
        result.Messages.First().MessageId.Should().Be("msg-duplicate");
        result.Messages.First().CorrelationId.Should().Be("correlation-1"); // First one wins
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_StopsAfterThreeAttemptsWithoutNewMessages()
    {
        // Arrange - Simulate situation where messages keep appearing but are duplicates
        var message = CreateTestMessage("msg-1", "correlation-1");

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "10"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(10);

        // Assert
        result.Messages.Should().HaveCount(1); // Only unique message

        // Should stop after 3 attempts (1 successful + 3 with no new messages = 4 total)
        _mockSqs.Verify(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_RestoresVisibilityOnException()
    {
        // Arrange
        var message = CreateTestMessage("msg-1", "correlation-1");

        var receiveCallCount = 0;
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                receiveCallCount++;
                if (receiveCallCount == 1)
                    return new ReceiveMessageResponse { Messages = new List<Message> { message } };
                throw new AmazonSQSException("Network error");
            });

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "10"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AmazonSQSException>(() => sut.PeekDeadLetterMessagesAsync(10));

        // Verify visibility was restored even though exception occurred
        _mockSqs.Verify(x => x.ChangeMessageVisibilityAsync(
            It.Is<ChangeMessageVisibilityRequest>(r =>
                r.QueueUrl == _dlqUrl &&
                r.ReceiptHandle == message.ReceiptHandle &&
                r.VisibilityTimeout == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_WhenVisibilityRestorationFails_LogsWarning()
    {
        // Arrange
        var message = CreateTestMessage("msg-1", "correlation-1");

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Receipt handle expired"));

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "1"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(1);

        // Assert
        result.Should().NotBeNull();

        // Should log warning about visibility restoration failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to restore visibility")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PeekDeadLetterMessagesAsync_ProcessesMessagesInBatches()
    {
        // Arrange - Request 25 messages, should make 3 batches (10+10+5)
        var batch1 = Enumerable.Range(1, 10).Select(i => CreateTestMessage($"msg-{i}", $"correlation-{i}")).ToList();
        var batch2 = Enumerable.Range(11, 10).Select(i => CreateTestMessage($"msg-{i}", $"correlation-{i}")).ToList();
        var batch3 = Enumerable.Range(21, 5).Select(i => CreateTestMessage($"msg-{i}", $"correlation-{i}")).ToList();

        var receiveCallCount = 0;
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                receiveCallCount++;
                return receiveCallCount switch
                {
                    1 => new ReceiveMessageResponse { Messages = batch1 },
                    2 => new ReceiveMessageResponse { Messages = batch2 },
                    3 => new ReceiveMessageResponse { Messages = batch3 },
                    _ => new ReceiveMessageResponse { Messages = new List<Message>() }
                };
            });

        _mockSqs.Setup(x => x.ChangeMessageVisibilityAsync(
            It.IsAny<ChangeMessageVisibilityRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "25"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PeekDeadLetterMessagesAsync(25);

        // Assert
        result.Messages.Should().HaveCount(25);

        // Verify correct batch sizes were requested
        _mockSqs.Verify(x => x.ReceiveMessageAsync(
            It.Is<ReceiveMessageRequest>(r => r.MaxNumberOfMessages == 10),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        _mockSqs.Verify(x => x.ReceiveMessageAsync(
            It.Is<ReceiveMessageRequest>(r => r.MaxNumberOfMessages == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_SuccessfullyRedrivesMessages()
    {
        // Arrange
        var message1 = CreateTestMessage("msg-1", "correlation-1");
        var message2 = CreateTestMessage("msg-2", "correlation-2");

        var receiveCallCount = 0;
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.Is<ReceiveMessageRequest>(r => r.QueueUrl == _dlqUrl),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                receiveCallCount++;
                return receiveCallCount switch
                {
                    1 => new ReceiveMessageResponse { Messages = new List<Message> { message1 } },
                    2 => new ReceiveMessageResponse { Messages = new List<Message> { message2 } },
                    _ => new ReceiveMessageResponse { Messages = new List<Message>() }
                };
            });

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r => r.QueueUrl == _queueUrl),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse());

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            _dlqUrl,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.RedriveDeadLetterMessagesAsync(2);

        // Assert
        result.Should().NotBeNull();
        result.MessagesRedriven.Should().Be(2);
        result.MessagesFailed.Should().Be(0);
        result.MessagesDuplicated.Should().Be(0);
        result.MessagesRemainingApprox.Should().Be(0);
        result.CorrelationIds.Should().Contain(new[] { "correlation-1", "correlation-2" });
        result.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_SendFails_IncrementsFailedCount()
    {
        // Arrange
        var message = CreateTestMessage("msg-1", "correlation-1");

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Queue not found"));

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "5"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.RedriveDeadLetterMessagesAsync(1);

        // Assert
        result.MessagesRedriven.Should().Be(0);
        result.MessagesFailed.Should().Be(1);
        result.MessagesDuplicated.Should().Be(0);

        _mockSqs.Verify(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_DeleteFails_IncrementsDuplicatedCount()
    {
        // Arrange
        var message = CreateTestMessage("msg-1", "correlation-1");

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse());

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Message no longer visible"));

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "1"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.RedriveDeadLetterMessagesAsync(1);

        // Assert
        result.MessagesRedriven.Should().Be(0);
        result.MessagesFailed.Should().Be(0);
        result.MessagesDuplicated.Should().Be(1);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CRITICAL")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_RemovesDlqAttributes()
    {
        // Arrange
        var message = CreateTestMessage("msg-1", "correlation-1");
        message.MessageAttributes["DLQ_FailureReason"] = new MessageAttributeValue { StringValue = "Error", DataType = "String" };
        message.MessageAttributes["DLQ_OriginalMessageId"] = new MessageAttributeValue { StringValue = "original-1", DataType = "String" };
        message.MessageAttributes["CustomAttribute"] = new MessageAttributeValue { StringValue = "Keep", DataType = "String" };

        SendMessageRequest? capturedRequest = null;

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new SendMessageResponse());

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        await sut.RedriveDeadLetterMessagesAsync(1);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.MessageAttributes.Should().NotContainKey("DLQ_FailureReason");
        capturedRequest.MessageAttributes.Should().NotContainKey("DLQ_OriginalMessageId");
        capturedRequest.MessageAttributes.Should().ContainKey("CustomAttribute");
        capturedRequest.MessageAttributes["CustomAttribute"].StringValue.Should().Be("Keep");
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_StopsWhenNoMoreMessages()
    {
        // Arrange
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.RedriveDeadLetterMessagesAsync(10);

        // Assert
        result.MessagesRedriven.Should().Be(0);
        result.MessagesFailed.Should().Be(0);

        _mockSqs.Verify(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_WithMixedResults_ReturnsCorrectSummary()
    {
        // Arrange
        var successMessage = CreateTestMessage("msg-success", "correlation-success");
        var failMessage = CreateTestMessage("msg-fail", "correlation-fail");
        var duplicateMessage = CreateTestMessage("msg-duplicate", "correlation-duplicate");

        var receiveCallCount = 0;
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.Is<ReceiveMessageRequest>(r => r.QueueUrl == _dlqUrl),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                receiveCallCount++;
                return receiveCallCount switch
                {
                    1 => new ReceiveMessageResponse { Messages = new List<Message> { successMessage } },
                    2 => new ReceiveMessageResponse { Messages = new List<Message> { failMessage } },
                    3 => new ReceiveMessageResponse { Messages = new List<Message> { duplicateMessage } },
                    _ => new ReceiveMessageResponse { Messages = new List<Message>() }
                };
            });

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r => r.MessageBody.Contains(successMessage.MessageId)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse());

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            _dlqUrl,
            successMessage.ReceiptHandle,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r => r.MessageBody.Contains(failMessage.MessageId)),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Network error"));

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(r => r.MessageBody.Contains(duplicateMessage.MessageId)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse());

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            _dlqUrl,
            duplicateMessage.ReceiptHandle,
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Delete failed"));

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "5"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.RedriveDeadLetterMessagesAsync(3);

        // Assert
        result.MessagesRedriven.Should().Be(1);
        result.MessagesFailed.Should().Be(1);
        result.MessagesDuplicated.Should().Be(1);
        result.MessagesRemainingApprox.Should().Be(5);
        result.CorrelationIds.Should().Contain(new[] { "correlation-success", "correlation-fail", "correlation-duplicate" });
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_WhenMaxMessagesIsZero_RedrivesAllUpToConfiguredMax()
    {
        // Arrange
        var messages = Enumerable.Range(1, 5)
            .Select(i => CreateTestMessage($"msg-{i}", $"correlation-{i}"))
            .ToList();

        var receiveCallCount = 0;
        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.Is<ReceiveMessageRequest>(r => r.QueueUrl == _dlqUrl),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (receiveCallCount < messages.Count)
                    return new ReceiveMessageResponse { Messages = new List<Message> { messages[receiveCallCount++] } };
                return new ReceiveMessageResponse { Messages = new List<Message>() };
            });

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "500" // Large queue
                }
            });

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse());

        _mockSqs.Setup(x => x.DeleteMessageAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.RedriveDeadLetterMessagesAsync(0);

        // Assert
        result.MessagesRedriven.Should().Be(5);

        // Verify it logged the capping
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Redriving all") &&
                    v.ToString()!.Contains("500") &&
                    v.ToString()!.Contains("max allowed: 100")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_WhenMaxMessagesExceedsConfigured_ClampsAndLogsWarning()
    {
        // Arrange
        var customOptions = new IntakeEventQueueOptions
        {
            QueueUrl = _queueUrl,
            DeadLetterQueueUrl = _dlqUrl,
            MaxRedriveMessages = 50
        };

        _mockSqs.Setup(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });

        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(customOptions), _mockLogger.Object);

        // Act
        await sut.RedriveDeadLetterMessagesAsync(200);

        // Assert - Should be clamped to 50
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Requested to redrive 200 messages") &&
                    v.ToString()!.Contains("capped to configured maximum of 50")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RedriveDeadLetterMessagesAsync_WhenEmptyQueue_ReturnsEmptySummaryWithoutProcessing()
    {
        // Arrange
        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.RedriveDeadLetterMessagesAsync(0);

        // Assert
        result.MessagesRedriven.Should().Be(0);
        result.MessagesFailed.Should().Be(0);
        result.MessagesDuplicated.Should().Be(0);
        result.MessagesRemainingApprox.Should().Be(0);
        result.CorrelationIds.Should().BeEmpty();

        // Should not attempt to receive messages
        _mockSqs.Verify(x => x.ReceiveMessageAsync(
            It.IsAny<ReceiveMessageRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PurgeDeadLetterQueueAsync_SuccessfullyPurgesQueue()
    {
        // Arrange
        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            _dlqUrl,
            It.Is<List<string>>(attrs => attrs.Contains("ApproximateNumberOfMessages")),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "25"
                }
            });

        _mockSqs.Setup(x => x.PurgeQueueAsync(
            It.Is<PurgeQueueRequest>(r => r.QueueUrl == _dlqUrl),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurgeQueueResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PurgeDeadLetterQueueAsync();

        // Assert
        result.Should().NotBeNull();
        result.Purged.Should().BeTrue();
        result.ApproximateMessagesPurged.Should().Be(25);
        result.PurgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockSqs.Verify(x => x.PurgeQueueAsync(
            It.Is<PurgeQueueRequest>(r => r.QueueUrl == _dlqUrl),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeDeadLetterQueueAsync_LogsWarning()
    {
        // Arrange
        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "10"
                }
            });

        _mockSqs.Setup(x => x.PurgeQueueAsync(
            It.IsAny<PurgeQueueRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurgeQueueResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        await sut.PurgeDeadLetterQueueAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Purging dead letter queue") &&
                    v.ToString()!.Contains("destructive operation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PurgeDeadLetterQueueAsync_WithEmptyQueue_PurgesSuccessfully()
    {
        // Arrange
        _mockSqs.Setup(x => x.GetQueueAttributesAsync(
            It.IsAny<string>(),
            It.IsAny<List<string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = "0"
                }
            });

        _mockSqs.Setup(x => x.PurgeQueueAsync(
            It.IsAny<PurgeQueueRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurgeQueueResponse());

        var sut = new DeadLetterQueueService(_mockSqs.Object, Options.Create(_options), _mockLogger.Object);

        // Act
        var result = await sut.PurgeDeadLetterQueueAsync();

        // Assert
        result.Purged.Should().BeTrue();
        result.ApproximateMessagesPurged.Should().Be(0);
    }

    private Message CreateTestMessage(string? messageId = null, string? correlationId = null)
    {
        var message = new Message
        {
            MessageId = messageId ?? "msg-12345",
            ReceiptHandle = $"receipt-handle-{messageId ?? "xyz"}",
            Body = $"{{\"orderId\": \"order-123\", \"messageId\": \"{messageId}\", \"amount\": 99.99}}",
            Attributes = new Dictionary<string, string>
            {
                ["ApproximateReceiveCount"] = "2",
                ["SentTimestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            },
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
        };

        if (!string.IsNullOrEmpty(correlationId))
        {
            message.MessageAttributes["CorrelationId"] = new MessageAttributeValue
            {
                StringValue = correlationId,
                DataType = "String"
            };
        }

        return message;
    }
}