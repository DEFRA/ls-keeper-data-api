using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;

namespace KeeperData.Core.Messaging.Contracts.V1.Serializers;

public class PlaceholderMessageSerializer : IUnwrappedMessageSerializer<PlaceholderMessage>
{
    public PlaceholderMessage? Deserialize(UnwrappedMessage message)
    {
        var messageBody = JsonSerializer.Deserialize(message.Payload, PlaceholderMessageSerializerContext.Default.PlaceholderMessage);
        return messageBody;
    }
}
