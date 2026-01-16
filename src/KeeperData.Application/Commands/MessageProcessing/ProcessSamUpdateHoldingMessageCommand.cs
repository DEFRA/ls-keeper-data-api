using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessSamUpdateHoldingMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand, ITransactionalCommand;