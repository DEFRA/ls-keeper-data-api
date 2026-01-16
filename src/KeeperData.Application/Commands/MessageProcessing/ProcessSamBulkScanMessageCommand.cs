using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessSamBulkScanMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand;