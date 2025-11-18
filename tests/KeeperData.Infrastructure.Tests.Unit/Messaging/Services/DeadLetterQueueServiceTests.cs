using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Core.Exceptions;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Services
{
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
            Assert.True(result);

            // Verify visibility was extended
            _mockSqs.Verify(x => x.ChangeMessageVisibilityAsync(
                It.Is<ChangeMessageVisibilityRequest>(r =>
                    r.QueueUrl == _queueUrl &&
                    r.ReceiptHandle == message.ReceiptHandle &&
                    r.VisibilityTimeout == 300),
                It.IsAny<CancellationToken>()), Times.Once);

            // Verify send to DLQ
            _mockSqs.Verify(x => x.SendMessageAsync(
                It.Is<SendMessageRequest>(r =>
                    r.QueueUrl == _dlqUrl &&
                    r.MessageBody == message.Body &&
                    r.MessageAttributes.ContainsKey("DLQ_FailureReason") &&
                    r.MessageAttributes["DLQ_FailureReason"].StringValue == "NonRetryableException"),
                It.IsAny<CancellationToken>()), Times.Once);

            // Verify delete from main queue
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
            Assert.False(result);

            // Verify delete was never called
            _mockSqs.Verify(x => x.DeleteMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);

            // Verify error was logged
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
            Assert.False(result);

            // Verify critical error about duplicate was logged
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
            Assert.False(result);

            // Verify no SQS calls were made
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
            Assert.NotNull(capturedRequest);
            Assert.Contains("DLQ_FailureReason", capturedRequest.MessageAttributes.Keys);
            Assert.Equal("NonRetryableException", capturedRequest.MessageAttributes["DLQ_FailureReason"].StringValue);

            Assert.Contains("DLQ_FailureMessage", capturedRequest.MessageAttributes.Keys);
            Assert.Equal("Invalid data format", capturedRequest.MessageAttributes["DLQ_FailureMessage"].StringValue);

            Assert.Contains("DLQ_OriginalMessageId", capturedRequest.MessageAttributes.Keys);
            Assert.Equal(message.MessageId, capturedRequest.MessageAttributes["DLQ_OriginalMessageId"].StringValue);

            Assert.Contains("DLQ_ReceiveCount", capturedRequest.MessageAttributes.Keys);
            Assert.Equal("2", capturedRequest.MessageAttributes["DLQ_ReceiveCount"].StringValue);

            Assert.Contains("DLQ_FailureTimestamp", capturedRequest.MessageAttributes.Keys);
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
            Assert.NotNull(capturedRequest);
            Assert.Contains("CustomerId", capturedRequest.MessageAttributes.Keys);
            Assert.Equal("12345", capturedRequest.MessageAttributes["CustomerId"].StringValue);
            Assert.Contains("Priority", capturedRequest.MessageAttributes.Keys);
            Assert.Equal("High", capturedRequest.MessageAttributes["Priority"].StringValue);
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
            Assert.False(result);

            // Verify we didn't proceed to send/delete after visibility failure
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
            Assert.NotNull(capturedRequest);
            var failureMessage = capturedRequest.MessageAttributes["DLQ_FailureMessage"].StringValue;
            Assert.True(failureMessage.Length <= 256, $"Expected <= 256 but was {failureMessage.Length}");
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

        private Message CreateTestMessage()
        {
            return new Message
            {
                MessageId = "msg-12345",
                ReceiptHandle = "receipt-handle-xyz",
                Body = "{\"orderId\": \"order-123\", \"amount\": 99.99}",
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateReceiveCount"] = "2",
                    ["SentTimestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                },
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
            };
        }
    }
}