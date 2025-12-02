using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamUpdateHoldingMessageHandler(
    SamHoldingImportOrchestrator orchestrator,
    IUnwrappedMessageSerializer<SamUpdateHoldingMessage> serializer)
    : IMessageHandler<SamUpdateHoldingMessage>
{
    private readonly IUnwrappedMessageSerializer<SamUpdateHoldingMessage> _serializer = serializer;
    private readonly SamHoldingImportOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamUpdateHoldingMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new SamHoldingImportContext
        {
            Cph = messagePayload.Identifier,
            CurrentDateTime = DateTime.UtcNow
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}