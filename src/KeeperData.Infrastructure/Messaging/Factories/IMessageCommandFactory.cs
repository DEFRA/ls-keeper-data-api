using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.MessageHandlers;

namespace KeeperData.Infrastructure.Messaging.Factories;

public interface IMessageCommandFactory
{
    IMessageProcessingCommand Create(UnwrappedMessage message);
}