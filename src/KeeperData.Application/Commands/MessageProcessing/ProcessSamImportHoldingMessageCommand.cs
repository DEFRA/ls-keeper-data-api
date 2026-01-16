using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessSamImportHoldingMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand, ITransactionalCommand;