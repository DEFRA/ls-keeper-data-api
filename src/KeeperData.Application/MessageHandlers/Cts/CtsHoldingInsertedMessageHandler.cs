using KeeperData.Application.Orchestration.Cts.Inserts;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsHoldingInsertedMessageHandler(CtsHoldingInsertOrchestrator orchestrator,
  IUnwrappedMessageSerializer<CtsHoldingInsertedMessage> serializer)
  : IMessageHandler<CtsHoldingInsertedMessage>
{
    private readonly IUnwrappedMessageSerializer<CtsHoldingInsertedMessage> _serializer = serializer;
    private readonly CtsHoldingInsertOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(CtsHoldingInsertedMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new CtsHoldingInsertContext
        {
            Cph = messagePayload.Identifier,
            BatchId = messagePayload.BatchId
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}