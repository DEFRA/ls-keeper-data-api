using KeeperData.Core.Messaging.Serializers;
using System.Text.Json;

namespace KeeperData.Core.Messaging.Contracts.Serializers;

public class MessageIdentifierSerializer<T> : IUnwrappedMessageSerializer<T>
    where T : MessageType
{
    public T? Deserialize(UnwrappedMessage message)
    {
        try
        {
            return JsonSerializer.Deserialize(
                message.Payload,
                MessageIdentifierSerializerContext.Default.GetTypeInfo(typeof(T))!
            ) as T;
        }
        catch
        {
            return null;
        }
    }
}
