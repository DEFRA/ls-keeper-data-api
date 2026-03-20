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
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_SendSucceedsButDeleteFails_LogsCriticalError()
    {
        // Arrange
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Test failure");

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
    public async Task MoveToDeadLetterQueue_TruncatesLongExceptionMessage()
    {
        // Arrange
        var message = CreateTestMessage();
        var longMessage = new string('x', 500);
        var exception = new NonRetryableException(longMessage);
        SendMessageRequest? capturedRequest = null;

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
        failureMessage.Length.Should().BeLessOrEqualTo(256);
    }

    [Fact]
    public async Task MoveToDeadLetterQueue_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var message = CreateTestMessage();
        var exception = new NonRetryableException("Test failure");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockSqs.Setup(x => x.SendMessageAsync(
            It.IsAny<SendMessageRequest>(),
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
                r.MaxNumberOfMessages == 2),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = messages });

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

        // Assert - Should clamp to 10
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
        dto.MessageType.Should().Be("OrderCreated");
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