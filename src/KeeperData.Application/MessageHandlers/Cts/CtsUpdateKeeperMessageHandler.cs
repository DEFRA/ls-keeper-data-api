using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;
using System.Threading;
using System.Threading.Tasks;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsUpdateKeeperMessageHandler : IMessageHandler<CtsUpdateKeeperMessage>
{
    private readonly IUnwrappedMessageSerializer<CtsUpdateKeeperMessage> _serializer;

    public CtsUpdateKeeperMessageHandler(IUnwrappedMessageSerializer<CtsUpdateKeeperMessage> serializer)
    {
        _serializer = serializer;
    }

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(CtsUpdateKeeperMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        // TODO: Implement import orchestration in a future story
        return await Task.FromResult(messagePayload!);
    }
}