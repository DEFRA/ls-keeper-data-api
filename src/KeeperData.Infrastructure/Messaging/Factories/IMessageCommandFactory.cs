using KeeperData.Application.Commands;
using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Infrastructure.Messaging.Factories;

public interface IMessageCommandFactory
{
    IMessageProcessingCommand Create(UnwrappedMessage message);
}