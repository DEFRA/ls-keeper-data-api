using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Extensions;
using KeeperData.Infrastructure;
using System.Text.Json;

namespace KeeperData.Api.Tests.Component.Consumers.Helpers;

public class SNSMessageUtility
{
    internal static Message SetupMessageWithOriginSns<TMessage>(string messageId, string correlationId, string subject, TMessage message)
    {
        var messageSerialized = JsonSerializer.Serialize(message, JsonDefaults.DefaultOptionsWithStringEnumConversion);

        var snsEnvelope = new SnsEnvelope
        {
            Type = "Notification",
            Message = messageSerialized,
            MessageId = messageId,
            MessageAttributes = []
        };

        snsEnvelope.MessageAttributes.TryAdd("CorrelationId", new SnsMessageAttribute() { Type = "String", Value = correlationId });
        snsEnvelope.MessageAttributes.TryAdd("Subject", new SnsMessageAttribute() { Type = "String", Value = (subject ?? typeof(TMessage).Name).ReplaceSuffix() });

        var snsEnvelopeSerialized = JsonSerializer.Serialize(snsEnvelope, JsonDefaults.DefaultOptionsWithSnsPascalSupport);
        var serviceBusMessage = new Message { MessageId = messageId, ReceiptHandle = messageId, Body = snsEnvelopeSerialized, MessageAttributes = [] };

        return serviceBusMessage;
    }
}