using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Core.Messaging.Exceptions;
using KeeperData.Infrastructure.Messaging.Factories.Implementations;
using KeeperData.Infrastructure.Messaging.Publishers;
using KeeperData.Infrastructure.Messaging.Publishers.Configuration;
using Moq;
using System.Net;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.MessagePublishers;

public class MessagePublisherTests
{
    private readonly Mock<IAmazonSQS> _amazonSQSMock = new();
    private readonly Mock<IServiceBusSenderConfiguration> _serviceBusSenderConfigurationMock = new();
    private readonly MessageFactory _messageFactory = new();

    private readonly IntakeEventQueuePublisher _sut;

    private const string TestQueueUrl = "http://localhost:4566/000000000000/test-queue";

    public MessagePublisherTests()
    {
        _amazonSQSMock
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        SetupServiceBusSenderConfiguration(TestQueueUrl);

        _sut = new IntakeEventQueuePublisher(_amazonSQSMock.Object, _messageFactory, _serviceBusSenderConfigurationMock.Object);
    }

    [Fact]
    public async Task GivenValidMessage_WhenCallingPublishAsync_ShouldSucceed()
    {
        var testMessage = new TestPublishMessage { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        Func<Task> func = async () => await _sut.PublishAsync(testMessage, CancellationToken.None);

        await func.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GivenNullMessage_WhenCallingPublishAsync_ShouldThrow()
    {
        Func<Task> func = async () => await _sut.PublishAsync<TestPublishMessage>(null, CancellationToken.None);

        await func.Should().ThrowAsync<ArgumentException>().WithMessage("Message payload was null (Parameter 'message')");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GivenMissingQueueUrl_WhenCallingPublishAsync_ShouldThrow(string? queueUrl)
    {
        SetupServiceBusSenderConfiguration(queueUrl);

        var testMessage = new { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        Func<Task> func = async () => await _sut.PublishAsync(testMessage, CancellationToken.None);

        await func.Should().ThrowAsync<PublishFailedException>().WithMessage("QueueUrl is missing");
    }

    [Fact]
    public async Task GivenValidMessage_AndSnsServiceFails_WhenCallingPublishAsync_ShouldThrow()
    {
        _amazonSQSMock
            .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Queue not found"));

        var testMessage = new { Id = Guid.NewGuid(), Name = Guid.NewGuid().ToString() };

        Func<Task> func = async () => await _sut.PublishAsync(testMessage, CancellationToken.None);

        var exceptionAssertion = await func.Should().ThrowAsync<PublishFailedException>();
        exceptionAssertion.And.Message.Should().Be($"Failed to publish message on {TestQueueUrl}.");
        exceptionAssertion.And.InnerException.Should().BeOfType<NotFoundException>();
        exceptionAssertion.And.InnerException!.Message.Should().Be("Queue not found");
    }

    private void SetupServiceBusSenderConfiguration(string? queueUrl)
    {
        _serviceBusSenderConfigurationMock.Setup(c => c.IntakeEventQueue).Returns(new QueueConfiguration
        {
            QueueUrl = queueUrl!
        });
    }
}

public class TestPublishMessage
{
    public Guid Id { get; set; }
    public string? Name { get; set; } = string.Empty;
}