using KeeperData.Application.Orchestration.Imports.Sam.Holders;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamImportHolderMessageHandler(SamHolderImportOrchestrator orchestrator,
  IUnwrappedMessageSerializer<SamImportHolderMessage> serializer)
  : IMessageHandler<SamImportHolderMessage>
{
    private readonly IUnwrappedMessageSerializer<SamImportHolderMessage> _serializer = serializer;
    private readonly SamHolderImportOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamImportHolderMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new SamHolderImportContext
        {
            PartyId = messagePayload.Identifier,
            BatchId = messagePayload.BatchId,
            CurrentDateTime = DateTime.UtcNow
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}