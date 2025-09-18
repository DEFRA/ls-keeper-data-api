using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Core.Messaging.Extensions;

public static class SqsMessageUnwrapper
{
    public static UnwrappedMessage Unwrap(this Message message, IMessageSerializer<SnsEnvelope> messageSerializer)
    {
        ArgumentNullException.ThrowIfNull(message);

        var envelope = TryDeserializeEnvelope(messageSerializer, message);

        return envelope?.Type == "Notification"
            ? UnwrapFromEnvelope(envelope)
            : UnwrapFromRawMessage(message);
    }

    private static SnsEnvelope? TryDeserializeEnvelope(IMessageSerializer<SnsEnvelope> serializer, Message message)
    {
        try
        {
            return serializer.Deserialize(message);
        }
        catch
        {
            return null;
        }
    }

    private static UnwrappedMessage UnwrapFromEnvelope(SnsEnvelope envelope)
    {
        var subject = envelope.GetMessageAttributeValue<string>("Subject") ?? "Default";
        var correlationId = envelope.GetMessageAttributeValue<string>("CorrelationId") ?? string.Empty;

        return new UnwrappedMessage
        {
            MessageId = envelope.MessageId,
            CorrelationId = correlationId,
            Subject = subject.ReplaceSuffix(),
            Payload = envelope.Message,
            Attributes = envelope.MessageAttributes?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Value ?? string.Empty)
        };
    }

    private static UnwrappedMessage UnwrapFromRawMessage(Message message)
    {
        var subject = message.GetMessageAttributeValue<string>("Subject") ?? "Default";
        var correlationId = message.GetMessageAttributeValue<string>("CorrelationId") ?? string.Empty;

        return new UnwrappedMessage
        {
            MessageId = message.MessageId,
            CorrelationId = correlationId,
            Subject = subject.ReplaceSuffix(),
            Payload = message.Body ?? string.Empty,
            Attributes = message.MessageAttributes?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.StringValue ?? string.Empty)
        };
    }
}