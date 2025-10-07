using KeeperData.Application.Orchestration.Sam.Deletions;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamHoldingDeletedMessageHandler(SamHoldingDeleteOrchestrator orchestrator,
  IUnwrappedMessageSerializer<SamHoldingDeletedMessage> serializer)
  : IMessageHandler<SamHoldingDeletedMessage>
{
    private readonly IUnwrappedMessageSerializer<SamHoldingDeletedMessage> _serializer = serializer;
    private readonly SamHoldingDeleteOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamHoldingDeletedMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new SamHoldingDeleteContext
        {
            Cph = messagePayload.Identifier,
            BatchId = messagePayload.BatchId
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}
