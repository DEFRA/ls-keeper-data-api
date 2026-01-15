using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands;

public interface IMessageProcessingCommand : ICommand<MessageType>
{
}