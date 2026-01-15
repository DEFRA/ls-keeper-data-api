using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessCtsUpdateAgentMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand, ITransactionalCommand;