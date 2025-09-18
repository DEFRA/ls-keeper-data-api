using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Component.Consumers.Helpers;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;

namespace KeeperData.Api.Tests.Component.Consumers;

public class QueuePollerTests
{
    [Fact]
    public async Task GivenMessageOriginatesFromSns_WhenReceiveMessageCalled_ShouldCompleteMessage()
    {
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var messageText = Guid.NewGuid();

        var componentTestMessage = GetPlaceholderMessage(messageText.ToString());
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        var amazonSqsMock = new Mock<IAmazonSQS>();
        amazonSqsMock
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var factory = new AppWebApplicationFactory();
        factory.OverrideService(amazonSqsMock.Object);

        using var scope = factory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Payload) = await queuePollerObserver.MessageHandled;
        var payloadAsType = Payload as PlaceholderMessage;

        MessageId.Should().NotBeNull().And.Be(messageId);
        payloadAsType.Should().NotBeNull();
        payloadAsType.Message.Should().NotBeNull().And.Be(messageText.ToString());

        SQSMessageUtility.VerifyMessageWasCompleted(amazonSqsMock);
    }

    [Fact]
    public async Task GivenMessageOriginatesFromSqs_WhenReceiveMessageCalled_ShouldCompleteMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var messageText = Guid.NewGuid();

        var componentTestMessage = GetPlaceholderMessage(messageText.ToString());
        var messageArgs = GetMessageWithOriginSqsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        var amazonSqsMock = new Mock<IAmazonSQS>();
        amazonSqsMock
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var factory = new AppWebApplicationFactory();
        factory.OverrideService(amazonSqsMock.Object);

        using var scope = factory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Payload) = await queuePollerObserver.MessageHandled;
        var payloadAsType = Payload as PlaceholderMessage;

        MessageId.Should().NotBeNull().And.Be(messageId);
        payloadAsType.Should().NotBeNull();
        payloadAsType.Message.Should().NotBeNull().And.Be(messageText.ToString());

        SQSMessageUtility.VerifyMessageWasCompleted(amazonSqsMock);
    }

    private static PlaceholderMessage GetPlaceholderMessage(string message) => new()
    {
        Message = message
    };

    private static ReceiveMessageResponse GetReceiveMessageResponseArgs(Message message)
    {
        var receiveMessageResponse = SQSMessageUtility.CreateReceiveMessageResponse(message);
        return receiveMessageResponse;
    }

    private static Message GetMessageWithOriginSqsArgs(string messageId, string correlationId, PlaceholderMessage placeholderMessage)
    {
        var message = SQSMessageUtility.SetupMessageWithOriginSqs(messageId, correlationId, "Placeholder", placeholderMessage);
        return message;
    }

    private static Message GetMessageWithOriginSnsArgs(string messageId, string correlationId, PlaceholderMessage placeholderMessage)
    {
        var message = SNSMessageUtility.SetupMessageWithOriginSns(messageId, correlationId, "Placeholder", placeholderMessage);
        return message;
    }
}

//public class QueuePollerTests(AppTestFixture appTestFixture) : IClassFixture<AppTestFixture>
//{
//    private readonly AppTestFixture _appTestFixture = appTestFixture;
//    private TestQueuePollerObserver<MessageType>? _observer;

//    [Fact]
//    public async Task GivenMessageOriginatesFromSns_WhenReceiveMessageCalled_ShouldCompleteMessage()
//    {
//        // Arrange
//        var messageId = Guid.NewGuid().ToString();
//        var correlationId = Guid.NewGuid().ToString();
//        var messageText = Guid.NewGuid();

//        var componentTestMessage = GetPlaceholderMessage(messageText.ToString());
//        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
//        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

//        var amazonSqsMock = new Mock<IAmazonSQS>();
//        amazonSqsMock
//            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(receiveMessageResponseArgs)
//            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
//        amazonSqsMock
//            .Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK });

//        _appTestFixture.AppWebApplicationFactory.OverrideService(amazonSqsMock.Object);

//        // Act
//        await ExecuteTest();

//        // Assert
//        SQSMessageUtility.VerifyMessageWasCompleted(_appTestFixture.AppWebApplicationFactory.AmazonSQSMock);

//        var (MessageId, Payload) = await _observer!.MessageHandled;
//        var payloadAsType = Payload as PlaceholderMessage;
//        Assert.Equal(messageId, MessageId);
//        Assert.Equal(messageText.ToString(), payloadAsType!.Message);
//    }

//    [Fact]
//    public async Task GivenMessageOriginatesFromSqs_WhenReceiveMessageCalled_ShouldCompleteMessage()
//    {
//        // Arrange
//        var messageId = Guid.NewGuid().ToString();
//        var correlationId = Guid.NewGuid().ToString();
//        var messageText = Guid.NewGuid();

//        var componentTestMessage = GetPlaceholderMessage(messageText.ToString());
//        var messageArgs = GetMessageWithOriginSqsArgs(messageId, correlationId, componentTestMessage);
//        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

//        var amazonSqsMock = new Mock<IAmazonSQS>();
//        amazonSqsMock
//            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(receiveMessageResponseArgs)
//            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
//        amazonSqsMock
//            .Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
//            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK });

//        _appTestFixture.AppWebApplicationFactory.OverrideService(amazonSqsMock.Object);

//        // Act
//        await ExecuteTest();

//        // Assert
//        SQSMessageUtility.VerifyMessageWasCompleted(_appTestFixture.AppWebApplicationFactory.AmazonSQSMock);

//        var (MessageId, Payload) = await _observer!.MessageHandled;
//        var payloadAsType = Payload as PlaceholderMessage;
//        Assert.Equal(messageId, MessageId);
//        Assert.Equal(messageText.ToString(), payloadAsType!.Message);
//    }

//    private async Task ExecuteTest()
//    {
//        using var cts = new CancellationTokenSource();
//        using var scope = _appTestFixture.AppWebApplicationFactory.Server.Services.CreateAsyncScope();
//        var queuePollerMultiType = scope.ServiceProvider.GetRequiredService<IQueuePoller>();

//        _observer = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

//        await queuePollerMultiType.StartAsync(cts.Token);

//        await _observer.MessageHandled;
//    }

//    private static PlaceholderMessage GetPlaceholderMessage(string message) => new()
//    {
//        Message = message
//    };

//    private static ReceiveMessageResponse GetReceiveMessageResponseArgs(Message message)
//    {
//        var receiveMessageResponse = SQSMessageUtility.CreateReceiveMessageResponse(message);
//        return receiveMessageResponse;
//    }

//    private static Message GetMessageWithOriginSqsArgs(string messageId, string correlationId, PlaceholderMessage placeholderMessage)
//    {
//        var message = SQSMessageUtility.SetupMessageWithOriginSqs(messageId, correlationId, "Placeholder", placeholderMessage);
//        return message;
//    }

//    private static Message GetMessageWithOriginSnsArgs(string messageId, string correlationId, PlaceholderMessage placeholderMessage)
//    {
//        var message = SNSMessageUtility.SetupMessageWithOriginSns(messageId, correlationId, "Placeholder", placeholderMessage);
//        return message;
//    }
//}
