using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Core.Messaging.MessageHandlers;

public interface IMessageHandler<in TMessage> : IMessageHandler
    where TMessage : MessageType
{
    Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default);
}

public interface IMessageHandler
{
}
