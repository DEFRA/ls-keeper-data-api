using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Core.Messaging.Extensions;

public static class SqsMessageUnwrapper
{
    public static UnwrappedMessage Unwrap(this Message message, IMessageSerializer<SnsEnvelope> messageSerializer)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            var envelope = messageSerializer.Deserialize(message);
            if (envelope?.Type == "Notification")
            {
                return new UnwrappedMessage
                {
                    MessageId = envelope.MessageId,
                    CorrelationId = envelope.GetMessageAttributeValue<string>("CorrelationId") ?? string.Empty,
                    Subject = (envelope.GetMessageAttributeValue<string>("Subject") ?? "Default").ReplaceSuffix(),
                    Payload = envelope.Message,
                    Attributes = envelope.MessageAttributes?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Value ?? string.Empty)
                };
            }
        }
        catch
        {
        }

        return new UnwrappedMessage
        {
            MessageId = message.MessageId,
            CorrelationId = message.GetMessageAttributeValue<string>("CorrelationId") ?? string.Empty,
            Subject = (message.GetMessageAttributeValue<string>("Subject") ?? "Default").ReplaceSuffix(),
            Payload = message.Body ?? string.Empty,
            Attributes = message.MessageAttributes?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.StringValue ?? string.Empty)
        };
    }
}