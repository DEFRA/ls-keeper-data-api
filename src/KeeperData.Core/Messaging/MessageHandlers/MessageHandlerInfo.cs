namespace KeeperData.Core.Messaging.MessageHandlers;

public class MessageHandlerInfo
{
    public bool IsDynamic { get; }
    public Type HandlerType { get; }

    private MessageHandlerInfo(bool isDynamic, Type handlerType)
    {
        IsDynamic = isDynamic;
        HandlerType = handlerType;
    }

    public static MessageHandlerInfo Dynamic(Type handlerType)
    {
        return new MessageHandlerInfo(true, handlerType);
    }

    public static MessageHandlerInfo Typed(Type handlerType)
    {
        return new MessageHandlerInfo(false, handlerType);
    }
}
