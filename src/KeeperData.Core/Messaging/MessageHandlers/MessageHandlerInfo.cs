namespace KeeperData.Core.Messaging.MessageHandlers;

public class MessageHandlerInfo
{
    public Type HandlerType { get; }

    private MessageHandlerInfo(Type handlerType)
    {
        HandlerType = handlerType;
    }

    public static MessageHandlerInfo Typed(Type handlerType)
    {
        return new MessageHandlerInfo(handlerType);
    }
}