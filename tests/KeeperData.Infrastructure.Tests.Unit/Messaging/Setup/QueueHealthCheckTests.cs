using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Setup;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using System.Net;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.Setup;

public class QueueHealthCheckTests
{
    private readonly Mock<IAmazonSQS> _amazonSimpleQueueServiceMock;
    private readonly IntakeEventQueueOptions _intakeEventQueueOptions;
    private readonly HealthCheckContext _healthCheckContext = new();

    private readonly QueueHealthCheck<IntakeEventQueueOptions> _sut;

    public QueueHealthCheckTests()
    {
        _amazonSimpleQueueServiceMock = new Mock<IAmazonSQS>();

        _amazonSimpleQueueServiceMock
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });

        _intakeEventQueueOptions = new IntakeEventQueueOptions
        {
            QueueUrl = "http://sqs.eu-west-2.127.0.0.1:4566/000000000000/ls_keeper_data_intake_queue",
            WaitTimeSeconds = 5,
            MaxNumberOfMessages = 10
        };

        _sut = new QueueHealthCheck<IntakeEventQueueOptions>(_intakeEventQueueOptions, _amazonSimpleQueueServiceMock.Object);
    }

    [Fact]
    public async Task GivenValidQueueName_WhenCallingCheckHealthAsync_ShouldSucceed()
    {
        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task GivenQueueAttributesAreMissing_WhenCallingCheckHealthAsync_ShouldReturnDegraded()
    {
        _amazonSimpleQueueServiceMock
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null);

        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task GivenGetQueueAttributesFails_WhenCallingCheckHealthAsync_ShouldReturnUnhealthy()
    {
        _amazonSimpleQueueServiceMock
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service call failed."));

        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task GivenGetQueueAttributesTimesOut_WhenCallingCheckHealthAsync_ShouldReturnUnhealthy()
    {
        _amazonSimpleQueueServiceMock
            .Setup(x => x.GetQueueAttributesAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Task has been cancelled"));

        var result = await _sut.CheckHealthAsync(_healthCheckContext, CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().NotBeNull().And.BeOfType<TimeoutException>();
        result.Exception.Message.Should().Be($"The queue check was cancelled, probably because it timed out after {_intakeEventQueueOptions.WaitTimeSeconds} seconds");
    }
}