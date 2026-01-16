using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Core.Messaging;
using KeeperData.Core.Messaging.Exceptions;
using KeeperData.Infrastructure.Messaging.Factories;
using KeeperData.Infrastructure.Messaging.Publishers;
using KeeperData.Infrastructure.Messaging.Publishers.Configuration;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Publishers;

public class IntakeEventQueuePublisherTests
{
    private readonly Mock<IAmazonSQS> _sqsServiceMock = new();
    private readonly Mock<IMessageFactory> _messageFactoryMock = new();
    private readonly Mock<IServiceBusSenderConfiguration> _configurationMock = new();

    private IntakeEventQueuePublisher CreateSut()
    {
        var queueConfig = new QueueConfiguration { QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue" };
        _configurationMock.Setup(x => x.IntakeEventQueue).Returns(queueConfig);

        return new IntakeEventQueuePublisher(
            _sqsServiceMock.Object,
            _messageFactoryMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public void QueueUrl_ShouldReturnConfigurationValue_WhenCalled()
    {
        // Arrange
        var expectedQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";
        var queueConfig = new QueueConfiguration { QueueUrl = expectedQueueUrl };
        _configurationMock.Setup(x => x.IntakeEventQueue).Returns(queueConfig);

        var sut = CreateSut();

        // Act
        var result = sut.QueueUrl;

        // Assert
        result.Should().Be(expectedQueueUrl);
    }

    [Fact]
    public void TopicArn_ShouldReturnEmptyString_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.TopicArn;

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowArgumentException_WhenMessageIsNull()
    {
        // Arrange
        var sut = CreateSut();
        object? message = null;

        // Act
        var action = () => sut.PublishAsync(message);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Message payload was null (Parameter 'message')"); ;
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowPublishFailedException_WhenQueueUrlIsNull()
    {
        // Arrange
        var message = new { Content = "Test message" };
        var queueConfig = new QueueConfiguration { QueueUrl = null! };
        _configurationMock.Setup(x => x.IntakeEventQueue).Returns(queueConfig);

        var sut = new IntakeEventQueuePublisher(
            _sqsServiceMock.Object,
            _messageFactoryMock.Object,
            _configurationMock.Object);

        // Act
        var action = () => sut.PublishAsync(message);

        // Assert
        await action.Should().ThrowAsync<PublishFailedException>()
            .WithMessage("QueueUrl is missing"); ;
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowPublishFailedException_WhenQueueUrlIsEmpty()
    {
        // Arrange
        var message = new { Content = "Test message" };
        var queueConfig = new QueueConfiguration { QueueUrl = string.Empty };
        _configurationMock.Setup(x => x.IntakeEventQueue).Returns(queueConfig);

        var sut = new IntakeEventQueuePublisher(
            _sqsServiceMock.Object,
            _messageFactoryMock.Object,
            _configurationMock.Object);

        // Act
        var action = () => sut.PublishAsync(message);

        // Assert
        await action.Should().ThrowAsync<PublishFailedException>()
            .WithMessage("QueueUrl is missing"); ;
    }

    [Fact]
    public async Task PublishAsync_ShouldSendMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new { Content = "Test message" };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";
        var correlationId = "test-correlation-id";
        var sendRequest = new SendMessageRequest { QueueUrl = queueUrl, MessageBody = "serialized message" };

        var queueConfig = new QueueConfiguration { QueueUrl = queueUrl };
        _configurationMock.Setup(x => x.IntakeEventQueue).Returns(queueConfig);

        var sut = CreateSut();

        // Set up the correlation ID context
        CorrelationIdContext.Value = correlationId;

        var expectedAttributes = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };

        _messageFactoryMock
            .Setup(x => x.CreateSqsMessage(queueUrl, message, null, expectedAttributes))
            .Returns(sendRequest);

        _sqsServiceMock
            .Setup(x => x.SendMessageAsync(sendRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = "test-message-id" });

        // Act
        await sut.PublishAsync(message);

        // Assert
        _messageFactoryMock.Verify(x => x.CreateSqsMessage(queueUrl, message, null, expectedAttributes), Times.Once);
        _sqsServiceMock.Verify(x => x.SendMessageAsync(sendRequest, It.IsAny<CancellationToken>()), Times.Once);

        // Clean up
        CorrelationIdContext.Value = null;
    }

    [Fact]
    public async Task PublishAsync_ShouldGenerateCorrelationId_WhenNotSet()
    {
        // Arrange
        var message = new { Content = "Test message" };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";
        var sendRequest = new SendMessageRequest { QueueUrl = queueUrl, MessageBody = "serialized message" };

        var queueConfig = new QueueConfiguration { QueueUrl = queueUrl };
        _configurationMock.Setup(x => x.IntakeEventQueue).Returns(queueConfig);

        var sut = CreateSut();

        // Ensure correlation ID is not set
        CorrelationIdContext.Value = null;

        _messageFactoryMock
            .Setup(x => x.CreateSqsMessage(queueUrl, message, null, It.IsAny<Dictionary<string, string>>()))
            .Returns(sendRequest);

        _sqsServiceMock
            .Setup(x => x.SendMessageAsync(sendRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = "test-message-id" });

        // Act
        await sut.PublishAsync(message);

        // Assert
        _messageFactoryMock.Verify(x => x.CreateSqsMessage(
            queueUrl,
            message,
            null,
            It.Is<Dictionary<string, string>>(attrs => attrs.ContainsKey("CorrelationId") && !string.IsNullOrEmpty(attrs["CorrelationId"]))),
            Times.Once);
        _sqsServiceMock.Verify(x => x.SendMessageAsync(sendRequest, It.IsAny<CancellationToken>()), Times.Once);

        // Clean up
        CorrelationIdContext.Value = null;
    }

    [Fact]
    public async Task PublishAsync_ShouldUseCancellationToken_WhenProvided()
    {
        // Arrange
        var message = new { Content = "Test message" };
        var cancellationToken = new CancellationToken();
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";
        var sendRequest = new SendMessageRequest { QueueUrl = queueUrl, MessageBody = "serialized message" };

        var queueConfig = new QueueConfiguration { QueueUrl = queueUrl };
        _configurationMock.Setup(x => x.IntakeEventQueue).Returns(queueConfig);

        var sut = CreateSut();

        _messageFactoryMock
            .Setup(x => x.CreateSqsMessage(queueUrl, message, null, It.IsAny<Dictionary<string, string>>()))
            .Returns(sendRequest);

        _sqsServiceMock
            .Setup(x => x.SendMessageAsync(sendRequest, cancellationToken))
            .ReturnsAsync(new SendMessageResponse { MessageId = "test-message-id" });

        // Act
        await sut.PublishAsync(message, cancellationToken);

        // Assert
        _sqsServiceMock.Verify(x => x.SendMessageAsync(sendRequest, cancellationToken), Times.Once);

        // Clean up
        CorrelationIdContext.Value = null;
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowPublishFailedException_WhenSqsSendFails()
    {
        // Arrange
        var message = new { Content = "Test message" };
        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";
        var sendRequest = new SendMessageRequest { QueueUrl = queueUrl, MessageBody = "serialized message" };
        var innerException = new Exception("SQS service unavailable");

        var queueConfig = new QueueConfiguration { QueueUrl = queueUrl };
        _configurationMock.Setup(x => x.IntakeEventQueue).Returns(queueConfig);

        var sut = CreateSut();

        _messageFactoryMock
            .Setup(x => x.CreateSqsMessage(queueUrl, message, null, It.IsAny<Dictionary<string, string>>()))
            .Returns(sendRequest);

        _sqsServiceMock
            .Setup(x => x.SendMessageAsync(sendRequest, It.IsAny<CancellationToken>()))
            .ThrowsAsync(innerException);

        // Act
        var action = () => sut.PublishAsync(message);

        // Assert
        await action.Should().ThrowAsync<PublishFailedException>()
            .WithMessage($"Failed to publish message on {queueUrl}.");

        // Clean up
        CorrelationIdContext.Value = null;
    }
}