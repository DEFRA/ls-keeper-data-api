using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Core.Messaging.Serializers;

public interface IUnwrappedMessageSerializer<out T>
{
    T? Deserialize(UnwrappedMessage message);
}