using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using FluentAssertions;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Factories;
using KeeperData.Infrastructure.Messaging.Publishers;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Publishers;

public class BatchCompletionTopicPublisherTests
{
    private readonly Mock<IAmazonSimpleNotificationService> _snsServiceMock = new();
    private readonly Mock<IMessageFactory> _messageFactoryMock = new();
    private readonly Mock<IBatchCompletionNotificationConfiguration> _configurationMock = new();
    private readonly Mock<ILogger<BatchCompletionTopicPublisher>> _loggerMock = new();

    private BatchCompletionTopicPublisher CreateSut()
    {
        return new BatchCompletionTopicPublisher(
            _snsServiceMock.Object,
            _messageFactoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void TopicArn_ShouldReturnConfigurationValue_WhenCalled()
    {
        // Arrange
        var expectedTopicArn = "arn:aws:sns:us-east-1:123456789012:batch-completion-topic";
        var batchCompletionEventsTopic = new TopicConfiguration { TopicArn = expectedTopicArn };
        _configurationMock.Setup(x => x.BatchCompletionEventsTopic).Returns(batchCompletionEventsTopic);

        var sut = CreateSut();

        // Act
        var result = sut.TopicArn;

        // Assert
        result.Should().Be(expectedTopicArn);
    }

    [Fact]
    public void QueueUrl_ShouldReturnNull_WhenCalled()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.QueueUrl;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishMessage_WhenValidMessageProvided()
    {
        // Arrange
        var sut = CreateSut();
        var message = new { Content = "Test message" };
        var topicArn = "arn:aws:sns:us-east-1:123456789012:batch-completion-topic";
        var publishRequest = new PublishRequest { TopicArn = topicArn, Message = "serialized message" };

        var batchCompletionEventsTopic = new TopicConfiguration { TopicArn = topicArn };
        _configurationMock.Setup(x => x.BatchCompletionEventsTopic).Returns(batchCompletionEventsTopic);

        _messageFactoryMock
            .Setup(x => x.CreateSnsMessage(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(publishRequest);

        _snsServiceMock
            .Setup(x => x.PublishAsync(publishRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { MessageId = "test-message-id" });

        // Act
        await sut.PublishAsync(message);

        // Assert
        _messageFactoryMock.Verify(x => x.CreateSnsMessage(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        _snsServiceMock.Verify(x => x.PublishAsync(publishRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldUseCancellationToken_WhenProvided()
    {
        // Arrange
        var sut = CreateSut();
        var message = new { Content = "Test message" };
        var cancellationToken = new CancellationToken();
        var topicArn = "arn:aws:sns:us-east-1:123456789012:batch-completion-topic";
        var publishRequest = new PublishRequest { TopicArn = topicArn, Message = "serialized message" };

        var batchCompletionEventsTopic = new TopicConfiguration { TopicArn = topicArn };
        _configurationMock.Setup(x => x.BatchCompletionEventsTopic).Returns(batchCompletionEventsTopic);

        _messageFactoryMock
            .Setup(x => x.CreateSnsMessage(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(publishRequest);

        _snsServiceMock
            .Setup(x => x.PublishAsync(publishRequest, cancellationToken))
            .ReturnsAsync(new PublishResponse { MessageId = "test-message-id" });

        // Act
        await sut.PublishAsync(message, cancellationToken);

        // Assert
        _snsServiceMock.Verify(x => x.PublishAsync(publishRequest, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogWarning_WhenMessageIsNull()
    {
        // Arrange
        var sut = CreateSut();
        string? message = null;

        // Act
        await sut.PublishAsync(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempted to publish null message to batch completion topic")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _messageFactoryMock.Verify(x => x.CreateSnsMessage(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        _snsServiceMock.Verify(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogWarningAndReturn_WhenTopicArnIsNull()
    {
        // Arrange
        var sut = CreateSut();
        var message = new { Content = "Test message" };

        var batchCompletionEventsTopic = new TopicConfiguration { TopicArn = null };
        _configurationMock.Setup(x => x.BatchCompletionEventsTopic).Returns(batchCompletionEventsTopic);

        // Act
        await sut.PublishAsync(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Batch completion topic ARN is not configured, skipping SNS publish")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _messageFactoryMock.Verify(x => x.CreateSnsMessage(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        _snsServiceMock.Verify(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogWarningAndReturn_WhenTopicArnIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        var message = new { Content = "Test message" };

        var batchCompletionEventsTopic = new TopicConfiguration { TopicArn = null };
        _configurationMock.Setup(x => x.BatchCompletionEventsTopic).Returns(batchCompletionEventsTopic);

        // Act
        await sut.PublishAsync(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Batch completion topic ARN is not configured, skipping SNS publish")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _messageFactoryMock.Verify(x => x.CreateSnsMessage(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        _snsServiceMock.Verify(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogInformation_WhenMessagePublishedSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var message = new { Content = "Test message" };
        var topicArn = "arn:aws:sns:us-east-1:123456789012:batch-completion-topic";
        var publishRequest = new PublishRequest { TopicArn = topicArn, Message = "serialized message" };

        var batchCompletionEventsTopic = new TopicConfiguration { TopicArn = topicArn };
        _configurationMock.Setup(x => x.BatchCompletionEventsTopic).Returns(batchCompletionEventsTopic);

        _messageFactoryMock
            .Setup(x => x.CreateSnsMessage(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(publishRequest);

        _snsServiceMock
            .Setup(x => x.PublishAsync(publishRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse { MessageId = "test-message-id" });

        // Act
        await sut.PublishAsync(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully published batch completion message to SNS topic {topicArn}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogErrorAndNotThrow_WhenPublishFails()
    {
        // Arrange
        var sut = CreateSut();
        var message = new { Content = "Test message" };
        var topicArn = "arn:aws:sns:us-east-1:123456789012:batch-completion-topic";
        var publishRequest = new PublishRequest { TopicArn = topicArn, Message = "serialized message" };
        var exception = new Exception("SNS publish failed");

        var batchCompletionEventsTopic = new TopicConfiguration { TopicArn = topicArn };
        _configurationMock.Setup(x => x.BatchCompletionEventsTopic).Returns(batchCompletionEventsTopic);

        _messageFactoryMock
            .Setup(x => x.CreateSnsMessage(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns(publishRequest);

        _snsServiceMock
            .Setup(x => x.PublishAsync(publishRequest, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var action = () => sut.PublishAsync(message);

        // Assert
        await action.Should().NotThrowAsync();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to publish batch completion message to SNS topic {topicArn}")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}