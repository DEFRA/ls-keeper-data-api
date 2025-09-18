using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Extensions;
using KeeperData.Core.Messaging.MessageHandlers;

namespace KeeperData.Infrastructure.Messaging.MessageHandlers;

public class InMemoryMessageHandlerManager : IMessageHandlerManager
{
    private readonly Dictionary<string, List<MessageHandlerInfo>> _handlers;
    private readonly List<Type> _messageTypes;

    public InMemoryMessageHandlerManager()
    {
        _handlers = [];
        _messageTypes = [];
    }

    public void AddReceiver<T, TH>()
        where T : MessageType
        where TH : IMessageHandler<T>
    {
        var messageType = GetMessageTypeKey<T>();

        DoAddReceiver(typeof(TH), messageType, isDynamic: false);

        if (!_messageTypes.Contains(typeof(T)))
        {
            _messageTypes.Add(typeof(T));
        }
    }

    public Type GetMessageTypeByName(string messageType) => _messageTypes.SingleOrDefault(t => t.Name == messageType)!;

    public string GetMessageTypeKey<T>()
    {
        var messageName = typeof(T).Name;
        messageName = messageName.ReplaceSuffix();
        return messageName;
    }

    public bool HasHandlerForMessage(string messageType) => _handlers.ContainsKey(messageType);

    public bool HasHandlerForMessage<T>() where T : MessageType
    {
        var key = GetMessageTypeKey<T>();
        return HasHandlerForMessage(key);
    }

    public IEnumerable<MessageHandlerInfo> GetHandlersForMessage(string messageType) => _handlers[messageType];

    public IEnumerable<MessageHandlerInfo> GetHandlersForMessage<T>() where T : MessageType
    {
        var key = GetMessageTypeKey<T>();
        return GetHandlersForMessage(key);
    }

    private void DoAddReceiver(Type handlerType, string messageType, bool isDynamic)
    {
        if (!HasHandlerForMessage(messageType))
        {
            _handlers.Add(messageType, []);
        }

        if (_handlers[messageType].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException(
                $"Handler Type {handlerType.Name} already registered for '{messageType}'", nameof(handlerType));
        }

        if (isDynamic)
        {
            _handlers[messageType].Add(MessageHandlerInfo.Dynamic(handlerType));
        }
        else
        {
            _handlers[messageType].Add(MessageHandlerInfo.Typed(handlerType));
        }
    }
}
