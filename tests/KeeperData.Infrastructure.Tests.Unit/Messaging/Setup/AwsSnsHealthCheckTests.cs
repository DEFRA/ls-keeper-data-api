using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using FluentAssertions;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Setup;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using System.Net;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Setup;

public class AwsSnsHealthCheckTests
{
    private readonly Mock<IAmazonSimpleNotificationService> _amazonSimpleNotificationServiceMock;
    private readonly Mock<IBatchCompletionNotificationConfiguration> _batchCompletionNotificationConfigurationMock;
    private readonly HealthCheckContext _healthCheckContext = new();

    private readonly AwsSnsHealthCheck _sut;

    private const string BatchCompletionEventsTopicName = "ls-keeper-data-batch-completion-events";
    private const string BatchCompletionEventsTopicArn = $"arn:aws:sns:eu-west-2:000000000000:{BatchCompletionEventsTopicName}";

    public AwsSnsHealthCheckTests()
    {
        _amazonSimpleNotificationServiceMock = new Mock<IAmazonSimpleNotificationService>();

        _amazonSimpleNotificationServiceMock
            .Setup(x => x.ListTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTopicsResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Topics = [new Topic { TopicArn = BatchCompletionEventsTopicArn }]
            });

        _amazonSimpleNotificationServiceMock
            .Setup(x => x.GetTopicAttributesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTopicAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        _batchCompletionNotificationConfigurationMock = new Mock<IBatchCompletionNotificationConfiguration>();
        SetupBatchCompletionNotificationConfiguration(BatchCompletionEventsTopicName, BatchCompletionEventsTopicArn);

        _sut = new AwsSnsHealthCheck(
            _amazonSimpleNotificationServiceMock.Object,
            _batchCompletionNotificationConfigurationMock.Object);
    }

    [Fact]
    public async Task GivenValidTopicArn_WhenCallingCheckHealthAsync_ShouldSucceed()
    {
        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be($"SNS topic '{BatchCompletionEventsTopicName}' is reachable.");
        result.Data.Should().ContainKey("TopicArn");
        result.Data["TopicArn"].Should().Be(BatchCompletionEventsTopicArn);
    }

    [Fact]
    public async Task GivenValidTopicName_WhenCallingCheckHealthAsync_ShouldSucceed()
    {
        // Arrange
        SetupBatchCompletionNotificationConfiguration(BatchCompletionEventsTopicName, string.Empty);

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be($"SNS topic '{BatchCompletionEventsTopicName}' is reachable.");
        result.Data.Should().ContainKey("TopicArn");
        result.Data["TopicArn"].Should().Be(BatchCompletionEventsTopicArn);
    }

    [Fact]
    public async Task GivenListTopicsRequestFails_WhenCallingCheckHealthAsync_ShouldFail()
    {
        // Arrange
        SetupBatchCompletionNotificationConfiguration(BatchCompletionEventsTopicName, string.Empty);

        _amazonSimpleNotificationServiceMock
            .Setup(x => x.ListTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTopicsResponse { HttpStatusCode = HttpStatusCode.InternalServerError });

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("SNS ListTopics returned non-OK status.");
    }

    [Fact]
    public async Task GivenInvalidTopicName_WhenCallingCheckHealthAsync_ShouldFail()
    {
        // Arrange
        SetupBatchCompletionNotificationConfiguration("dummy-topic-name", string.Empty);

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("SNS topic 'dummy-topic-name' not found.");
    }

    [Fact]
    public async Task GivenGetTopicAttributesAsyncRequestFails_WhenCallingCheckHealthAsync_ShouldFail()
    {
        // Arrange
        SetupBatchCompletionNotificationConfiguration(BatchCompletionEventsTopicName, string.Empty);

        _amazonSimpleNotificationServiceMock
            .Setup(x => x.GetTopicAttributesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTopicAttributesResponse { HttpStatusCode = HttpStatusCode.InternalServerError });

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be($"SNS topic '{BatchCompletionEventsTopicName}' attributes fetch returned non-OK status.");
    }

    [Fact]
    public async Task GivenGetTopicAttributesAsyncRequestFailsWithTopicArn_WhenCallingCheckHealthAsync_ShouldFail()
    {
        // Arrange
        _amazonSimpleNotificationServiceMock
            .Setup(x => x.GetTopicAttributesAsync(BatchCompletionEventsTopicArn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTopicAttributesResponse { HttpStatusCode = HttpStatusCode.Forbidden });

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be($"SNS topic '{BatchCompletionEventsTopicName}' attributes fetch returned non-OK status.");
    }

    [Fact]
    public async Task GivenTopicNotFoundException_WhenCallingCheckHealthAsync_ShouldReturnUnhealthy()
    {
        // Arrange
        var notFoundException = new NotFoundException("Topic not found");
        _amazonSimpleNotificationServiceMock
            .Setup(x => x.GetTopicAttributesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(notFoundException);

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be($"SNS topic '{BatchCompletionEventsTopicName}' does not exist.");
        result.Exception.Should().Be(notFoundException);
    }

    [Fact]
    public async Task GivenUnexpectedException_WhenCallingCheckHealthAsync_ShouldReturnUnhealthy()
    {
        // Arrange
        var exception = new InvalidOperationException("Unexpected error");
        _amazonSimpleNotificationServiceMock
            .Setup(x => x.GetTopicAttributesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be($"Error accessing SNS topic '{BatchCompletionEventsTopicName}'.");
        result.Exception.Should().Be(exception);
    }

    [Fact]
    public async Task GivenListTopicsException_WhenCallingCheckHealthAsyncWithTopicName_ShouldReturnUnhealthy()
    {
        // Arrange
        SetupBatchCompletionNotificationConfiguration(BatchCompletionEventsTopicName, string.Empty);
        var exception = new AmazonSimpleNotificationServiceException("SNS service unavailable");

        _amazonSimpleNotificationServiceMock
            .Setup(x => x.ListTopicsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be($"Error accessing SNS topic '{BatchCompletionEventsTopicName}'.");
        result.Exception.Should().Be(exception);
    }

    [Fact]
    public void GivenNullSnsClient_WhenConstructingAwsSnsHealthCheck_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AwsSnsHealthCheck(null!, _batchCompletionNotificationConfigurationMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("snsClient");
    }

    [Fact]
    public void GivenNullConfiguration_WhenConstructingAwsSnsHealthCheck_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new AwsSnsHealthCheck(_amazonSimpleNotificationServiceMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public async Task GivenTopicNameWithDifferentCasing_WhenCallingCheckHealthAsync_ShouldSucceed()
    {
        // Arrange
        SetupBatchCompletionNotificationConfiguration(BatchCompletionEventsTopicName.ToUpperInvariant(), string.Empty);

        _amazonSimpleNotificationServiceMock
            .Setup(x => x.ListTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTopicsResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Topics = [new Topic { TopicArn = BatchCompletionEventsTopicArn.ToLowerInvariant() }]
            });

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be($"SNS topic '{BatchCompletionEventsTopicName.ToUpperInvariant()}' is reachable.");
    }

    [Fact]
    public async Task GivenMultipleTopicsInList_WhenCallingCheckHealthAsync_ShouldFindCorrectTopic()
    {
        // Arrange
        SetupBatchCompletionNotificationConfiguration(BatchCompletionEventsTopicName, string.Empty);

        var topicArns = new[]
        {
            "arn:aws:sns:eu-west-2:000000000000:other-topic-1",
            BatchCompletionEventsTopicArn,
            "arn:aws:sns:eu-west-2:000000000000:other-topic-2"
        };

        _amazonSimpleNotificationServiceMock
            .Setup(x => x.ListTopicsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListTopicsResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Topics = topicArns.Select(arn => new Topic { TopicArn = arn }).ToList()
            });

        // Act
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["TopicArn"].Should().Be(BatchCompletionEventsTopicArn);
    }

    private void SetupBatchCompletionNotificationConfiguration(string topicName, string topicArn = "")
    {
        _batchCompletionNotificationConfigurationMock.Setup(c => c.BatchCompletionEventsTopic).Returns(new TopicConfiguration
        {
            TopicName = topicName,
            TopicArn = topicArn
        });
    }
}