using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessCtsImportHoldingMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand, ITransactionalCommand;