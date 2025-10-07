using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace KeeperData.Core.Messaging.Contracts.Serializers;

public class MessageIdentifierSerializer<T>(JsonTypeInfo<T> typeInfo) : IUnwrappedMessageSerializer<T>
    where T : MessageType
{
    private readonly JsonTypeInfo<T> _typeInfo = typeInfo;

    public T? Deserialize(UnwrappedMessage message)
    {
        try
        {
            return JsonSerializer.Deserialize(message.Payload, _typeInfo);
        }
        catch
        {
            return null;
        }
    }
}