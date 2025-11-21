using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamUpdateHerdMessageHandler(IUnwrappedMessageSerializer<SamUpdateHerdMessage> serializer)
    : IMessageHandler<SamUpdateHerdMessage>
{
    private readonly IUnwrappedMessageSerializer<SamUpdateHerdMessage> _serializer = serializer;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamUpdateHerdMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        return await Task.FromResult(messagePayload!);
    }
}