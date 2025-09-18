using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers;

public class PlaceholderMessageHandler(IUnwrappedMessageSerializer<PlaceholderMessage> serializer) : IMessageHandler<PlaceholderMessage>
{
    private readonly IUnwrappedMessageSerializer<PlaceholderMessage> _serializer = serializer;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message);

        // Do something with the messagePayload

        return await Task.FromResult(messagePayload!);
    }
}