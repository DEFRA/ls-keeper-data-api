using KeeperData.Core.Messaging.Contracts;
using MediatR;

namespace KeeperData.Core.Messaging.MessageHandlers;

public interface IMessageProcessingCommand : IRequest<MessageType>
{
}