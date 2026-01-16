using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Core.Messaging.Contracts;

namespace KeeperData.Infrastructure.Messaging.Factories.Implementations;

public sealed class MessageCommandRegistry
{
    private readonly Dictionary<string, IMessageCommandFactory> _map = [];

    public void Register<TFactory>(string subject)
        where TFactory : IMessageCommandFactory, new()
    {
        _map[subject] = new TFactory();
    }

    public IMessageProcessingCommand CreateCommand(UnwrappedMessage message)
    {
        if (!_map.TryGetValue(message.Subject, out var factory))
            throw new InvalidOperationException($"No command registered for subject {message.Subject}");

        return factory.Create(message);
    }
}

public sealed class SamBulkScanMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessSamBulkScanMessageCommand(message);
}

public sealed class SamDailyScanMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessSamDailyScanMessageCommand(message);
}

public sealed class CtsBulkScanMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessCtsBulkScanMessageCommand(message);
}

public sealed class CtsDailyScanMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessCtsDailyScanMessageCommand(message);
}

public sealed class SamImportHoldingCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessSamImportHoldingMessageCommand(message);
}

public sealed class SamUpdateHoldingMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessSamUpdateHoldingMessageCommand(message);
}

public sealed class CtsImportHoldingMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessCtsImportHoldingMessageCommand(message);
}

public sealed class CtsUpdateHoldingMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessCtsUpdateHoldingMessageCommand(message);
}

public sealed class CtsUpdateKeeperMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessCtsUpdateKeeperMessageCommand(message);
}

public sealed class CtsUpdateAgentMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessCtsUpdateAgentMessageCommand(message);
}

public sealed class BatchCompletionMessageCommandFactory : IMessageCommandFactory
{
    public IMessageProcessingCommand Create(UnwrappedMessage message)
        => new ProcessBatchCompletionMessageCommand(message);
}