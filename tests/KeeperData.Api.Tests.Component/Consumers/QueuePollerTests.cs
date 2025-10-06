using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Component.Consumers.Helpers;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute.ExceptionExtensions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Consumers;

public class QueuePollerTests
{
    [Fact]
    public async Task GivenMessageOriginatesFromSns_WhenReceiveMessageCalled_ShouldCompleteMessage()
    {
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var identifierId = Guid.NewGuid();

        var componentTestMessage = GetSamCphHoldingImportedMessage(identifierId.ToString());
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        var amazonSqsMock = new Mock<IAmazonSQS>();
        amazonSqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsSingleton(amazonSqsMock.Object);
        factory.OverrideServiceAsTransient<IMessageHandler<SamCphHoldingImportedMessage>, TestSamCphHoldingImportedMessage>();

        using var scope = factory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Payload) = await queuePollerObserver.MessageHandled;
        var payloadAsType = Payload as SamCphHoldingImportedMessage;

        MessageId.Should().NotBeNull().And.Be(messageId);
        payloadAsType.Should().NotBeNull();
        payloadAsType.Identifier.Should().NotBeNull().And.Be(identifierId.ToString());

        SQSMessageUtility.VerifyMessageWasCompleted(amazonSqsMock);
    }

    [Fact]
    public async Task GivenMessageOriginatesFromSqs_WhenReceiveMessageCalled_ShouldCompleteMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var identifierId = Guid.NewGuid();

        var componentTestMessage = GetSamCphHoldingImportedMessage(identifierId.ToString());
        var messageArgs = GetMessageWithOriginSqsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        var amazonSqsMock = new Mock<IAmazonSQS>();
        amazonSqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsSingleton(amazonSqsMock.Object);
        factory.OverrideServiceAsTransient<IMessageHandler<SamCphHoldingImportedMessage>, TestSamCphHoldingImportedMessage>();

        using var scope = factory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Payload) = await queuePollerObserver.MessageHandled;
        var payloadAsType = Payload as SamCphHoldingImportedMessage;

        MessageId.Should().NotBeNull().And.Be(messageId);
        payloadAsType.Should().NotBeNull();
        payloadAsType.Identifier.Should().NotBeNull().And.Be(identifierId.ToString());

        SQSMessageUtility.VerifyMessageWasCompleted(amazonSqsMock);
    }

    [Fact]
    public async Task GivenValidMessage_WhenMessageHandlerReturnsTemporaryFailure_ThenShouldCallOnMessageFailed()
    {
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var identifierId = Guid.NewGuid();

        var componentTestMessage = GetSamCphHoldingImportedMessage(identifierId.ToString());
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        var amazonSqsMock = new Mock<IAmazonSQS>();
        amazonSqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });

        var samCphHoldingImportedMessageMock = new Mock<IMessageHandler<SamCphHoldingImportedMessage>>();
        samCphHoldingImportedMessageMock
            .Setup(x => x.Handle(It.IsAny<UnwrappedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("A temporary failure has occurred"));

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsSingleton(amazonSqsMock.Object);
        factory.OverrideServiceAsSingleton(samCphHoldingImportedMessageMock.Object);

        using var scope = factory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Exception) = await queuePollerObserver.MessageFailed;

        MessageId.Should().NotBeNull().And.Be(messageId);
        Exception.Should().BeOfType<RetryableException>();
        Exception.Message.Should().Be("A temporary failure has occurred");
    }

    [Fact]
    public async Task GivenValidMessage_WhenMessageHandlerReturnsPermanentFailure_ThenShouldCallOnMessageFailed()
    {
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var identifierId = Guid.NewGuid();

        var componentTestMessage = GetSamCphHoldingImportedMessage(identifierId.ToString());
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        var amazonSqsMock = new Mock<IAmazonSQS>();
        amazonSqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });

        var samCphHoldingImportedMessageMock = new Mock<IMessageHandler<SamCphHoldingImportedMessage>>();
        samCphHoldingImportedMessageMock
            .Setup(x => x.Handle(It.IsAny<UnwrappedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("A permanent failure has occurred"));

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsSingleton(amazonSqsMock.Object);
        factory.OverrideServiceAsSingleton(samCphHoldingImportedMessageMock.Object);

        using var scope = factory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Exception) = await queuePollerObserver.MessageFailed;

        MessageId.Should().NotBeNull().And.Be(messageId);
        Exception.Should().BeOfType<NonRetryableException>();
        Exception.Message.Should().Be("A permanent failure has occurred");
    }

    [Fact]
    public async Task GivenValidMessage_WhenNoMessageHandlerIsRegistered_ThenShouldCallOnMessageFailed()
    {
        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var messageText = Guid.NewGuid();

        var componentTestMessage = new QueuePollerTestMessage();
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        var amazonSqsMock = new Mock<IAmazonSQS>();
        amazonSqsMock
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        amazonSqsMock
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });

        var factory = new AppWebApplicationFactory();
        factory.OverrideServiceAsSingleton(amazonSqsMock.Object);

        using var scope = factory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Exception) = await queuePollerObserver.MessageFailed;

        MessageId.Should().NotBeNull().And.Be(messageId);
        Exception.Should().BeOfType<KeyNotFoundException>();
        Exception.Message.Should().Be("The given key 'QueuePollerTest' was not present in the dictionary.");
    }

    private static SamCphHoldingImportedMessage GetSamCphHoldingImportedMessage(string identifier) => new()
    {
        Identifier = identifier
    };

    private static ReceiveMessageResponse GetReceiveMessageResponseArgs(Message message)
    {
        var receiveMessageResponse = SQSMessageUtility.CreateReceiveMessageResponse(message);
        return receiveMessageResponse;
    }

    private static Message GetMessageWithOriginSqsArgs<TMessage>(string messageId, string correlationId, TMessage placeholderMessage)
    {
        var message = SQSMessageUtility.SetupMessageWithOriginSqs(messageId, correlationId, typeof(TMessage).Name, placeholderMessage);
        return message;
    }

    private static Message GetMessageWithOriginSnsArgs<TMessage>(string messageId, string correlationId, TMessage placeholderMessage)
    {
        var message = SNSMessageUtility.SetupMessageWithOriginSns(messageId, correlationId, typeof(TMessage).Name, placeholderMessage);
        return message;
    }

    public class QueuePollerTestMessage : MessageType { }

    public class TestSamCphHoldingImportedMessage() : IMessageHandler<SamCphHoldingImportedMessage>
    {
        public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));

            var messagePayload = System.Text.Json.JsonSerializer.Deserialize<SamCphHoldingImportedMessage>(message.Payload, JsonDefaults.DefaultOptions);

            return await Task.FromResult(messagePayload!);
        }
    }
}