using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessCtsUpdateHoldingMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand, ITransactionalCommand;