using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Extensions;
using KeeperData.Infrastructure;
using Moq;
using System.Text.Json;

namespace KeeperData.Api.Tests.Component.Consumers.Helpers;

public class SQSMessageUtility
{
    internal static ReceiveMessageResponse CreateReceiveMessageResponse(Message message)
    {
        var receiveMessageResponse = new ReceiveMessageResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, Messages = [message] };
        return receiveMessageResponse;
    }

    internal static ReceiveMessageResponse CreateReceiveMessageResponse(List<Message> messages)
    {
        var receiveMessageResponse = new ReceiveMessageResponse { HttpStatusCode = System.Net.HttpStatusCode.OK, Messages = messages };
        return receiveMessageResponse;
    }

    internal static Message SetupMessageWithOriginSqs<TMessage>(string messageId, string correlationId, string subject, TMessage message)
    {
        var messageSerialized = JsonSerializer.Serialize(message, JsonDefaults.DefaultOptionsWithStringEnumConversion);
        var serviceBusMessage = new Message { MessageId = messageId, ReceiptHandle = messageId, Body = messageSerialized, MessageAttributes = [] };

        serviceBusMessage.MessageAttributes.TryAdd("CorrelationId", new MessageAttributeValue() { DataType = "String", StringValue = correlationId });
        serviceBusMessage.MessageAttributes.TryAdd("Subject", new MessageAttributeValue() { DataType = "String", StringValue = (subject ?? typeof(TMessage).Name).ReplaceSuffix() });

        return serviceBusMessage;
    }

    internal static void VerifyMessageWasProcessed(Mock<IAmazonSQS>? sqsClientMock)
    {
        sqsClientMock?.Verify(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    internal static void VerifyMessageWasCompleted(Mock<IAmazonSQS>? sqsClientMock)
    {
        sqsClientMock?.Verify(x => x.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
