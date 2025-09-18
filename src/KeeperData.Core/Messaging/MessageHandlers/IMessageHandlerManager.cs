using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Core.Messaging.MessageHandlers;

public interface IMessageHandlerManager
{
    void AddReceiver<T, TH>()
        where T : MessageType
        where TH : IMessageHandler<T>;

    Type GetMessageTypeByName(string messageType);

    string GetMessageTypeKey<T>();

    bool HasHandlerForMessage(string messageType);

    bool HasHandlerForMessage<T>() where T : MessageType;

    IEnumerable<MessageHandlerInfo> GetHandlersForMessage(string messageType);

    IEnumerable<MessageHandlerInfo> GetHandlersForMessage<T>() where T : MessageType;
}
