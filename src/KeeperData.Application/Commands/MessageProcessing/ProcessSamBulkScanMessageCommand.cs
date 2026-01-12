using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.MessageHandlers;

namespace KeeperData.Application.Commands.MessageProcessing;

public sealed record ProcessSamBulkScanMessageCommand(UnwrappedMessage Message)
    : IMessageProcessingCommand;