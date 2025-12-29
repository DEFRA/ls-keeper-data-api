using Amazon.SQS.Model;
using FluentAssertions;
using KeeperData.Api.Tests.Component.Consumers.Helpers;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute.ExceptionExtensions;
using System.Net;

namespace KeeperData.Api.Tests.Component.Consumers;

[Collection("QueuePollerTests")]
public class QueuePollerTests : IClassFixture<AppTestFixture>
{
    private readonly AppTestFixture _appTestFixture;

    public QueuePollerTests(AppTestFixture appTestFixture)
    {
        _appTestFixture = appTestFixture;
    }

    [Fact]
    public async Task GivenMessageOriginatesFromSns_WhenReceiveMessageCalled_ShouldCompleteMessage()
    {
        _appTestFixture.AppWebApplicationFactory.ResetMocks();

        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var identifierId = Guid.NewGuid();

        var componentTestMessage = GetSamCphHoldingImportedMessage(identifierId.ToString());
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        _appTestFixture.AppWebApplicationFactory._samImportHoldingMessageHandlerMock.Setup(
            x => x.Handle(It.IsAny<UnwrappedMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(componentTestMessage);

        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Payload) = await queuePollerObserver.MessageHandled;
        var payloadAsType = Payload as SamImportHoldingMessage;

        MessageId.Should().NotBeNull().And.Be(messageId);
        payloadAsType.Should().NotBeNull();
        payloadAsType.Identifier.Should().NotBeNull().And.Be(identifierId.ToString());

        SQSMessageUtility.VerifyMessageWasCompleted(_appTestFixture.AppWebApplicationFactory.AmazonSQSMock!);
    }

    [Fact]
    public async Task GivenMessageOriginatesFromSqs_WhenReceiveMessageCalled_ShouldCompleteMessage()
    {
        _appTestFixture.AppWebApplicationFactory.ResetMocks();

        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var identifierId = Guid.NewGuid();

        var componentTestMessage = GetSamCphHoldingImportedMessage(identifierId.ToString());
        var messageArgs = GetMessageWithOriginSqsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .Setup(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = HttpStatusCode.OK });

        _appTestFixture.AppWebApplicationFactory._samImportHoldingMessageHandlerMock.Setup(
            x => x.Handle(It.IsAny<UnwrappedMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(componentTestMessage);

        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Payload) = await queuePollerObserver.MessageHandled;
        var payloadAsType = Payload as SamImportHoldingMessage;

        MessageId.Should().NotBeNull().And.Be(messageId);
        payloadAsType.Should().NotBeNull();
        payloadAsType.Identifier.Should().NotBeNull().And.Be(identifierId.ToString());

        SQSMessageUtility.VerifyMessageWasCompleted(_appTestFixture.AppWebApplicationFactory.AmazonSQSMock!);
    }

    [Fact]
    public async Task GivenValidMessage_WhenMessageHandlerReturnsTemporaryFailure_ThenShouldCallOnMessageFailed()
    {
        _appTestFixture.AppWebApplicationFactory.ResetMocks();

        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var identifierId = Guid.NewGuid();

        var componentTestMessage = GetSamCphHoldingImportedMessage(identifierId.ToString());
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });

        _appTestFixture.AppWebApplicationFactory._samImportHoldingMessageHandlerMock
            .Setup(x => x.Handle(It.IsAny<UnwrappedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RetryableException("A temporary failure has occurred"));

        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
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
        _appTestFixture.AppWebApplicationFactory.ResetMocks();

        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var identifierId = Guid.NewGuid();

        var componentTestMessage = GetSamCphHoldingImportedMessage(identifierId.ToString());
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });

        _appTestFixture.AppWebApplicationFactory._samImportHoldingMessageHandlerMock
            .Setup(x => x.Handle(It.IsAny<UnwrappedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NonRetryableException("A permanent failure has occurred"));

        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
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
        _appTestFixture.AppWebApplicationFactory.ResetMocks();

        var messageId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var messageText = Guid.NewGuid();

        var componentTestMessage = new QueuePollerTestMessage();
        var messageArgs = GetMessageWithOriginSnsArgs(messageId, correlationId, componentTestMessage);
        var receiveMessageResponseArgs = GetReceiveMessageResponseArgs(messageArgs);

        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });
        _appTestFixture.AppWebApplicationFactory.AmazonSQSMock!
            .SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiveMessageResponseArgs)
            .ReturnsAsync(new ReceiveMessageResponse { HttpStatusCode = HttpStatusCode.OK, Messages = [] });

        await using var scope = _appTestFixture.AppWebApplicationFactory.Services.CreateAsyncScope();
        var queuePoller = scope.ServiceProvider.GetRequiredService<IQueuePoller>();
        var queuePollerObserver = scope.ServiceProvider.GetRequiredService<TestQueuePollerObserver<MessageType>>();

        using var cts = new CancellationTokenSource();
        await queuePoller.StartAsync(cts.Token);

        var (MessageId, Exception) = await queuePollerObserver.MessageFailed;

        MessageId.Should().NotBeNull().And.Be(messageId);
        Exception.Should().BeOfType<KeyNotFoundException>();
        Exception.Message.Should().Be("The given key 'QueuePollerTest' was not present in the dictionary.");
    }

    private static SamImportHoldingMessage GetSamCphHoldingImportedMessage(string identifier) => new()
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
}