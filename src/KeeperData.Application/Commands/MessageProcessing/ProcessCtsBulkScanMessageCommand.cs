using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessCtsBulkScanMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand;