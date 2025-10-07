using KeeperData.Application.Orchestration.Cts.Deletions;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsHoldingDeletedMessageHandler(CtsHoldingDeletedOrchestrator orchestrator,
  IUnwrappedMessageSerializer<CtsHoldingDeletedMessage> serializer)
  : IMessageHandler<CtsHoldingDeletedMessage>
{
    private readonly IUnwrappedMessageSerializer<CtsHoldingDeletedMessage> _serializer = serializer;
    private readonly CtsHoldingDeletedOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(CtsHoldingDeletedMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new CtsHoldingDeleteContext
        {
            Cph = messagePayload.Identifier,
            BatchId = messagePayload.BatchId
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}
