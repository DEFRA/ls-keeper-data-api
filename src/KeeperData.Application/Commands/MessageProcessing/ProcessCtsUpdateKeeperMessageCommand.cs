using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessCtsUpdateKeeperMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand, ITransactionalCommand;