using KeeperData.Application.Orchestration.Sam.Deletions;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamPartyDeletedMessageHandler(SamPartyDeleteOrchestrator orchestrator,
  IUnwrappedMessageSerializer<SamPartyDeletedMessage> serializer)
  : IMessageHandler<SamPartyDeletedMessage>
{
    private readonly IUnwrappedMessageSerializer<SamPartyDeletedMessage> _serializer = serializer;
    private readonly SamPartyDeleteOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamPartyDeletedMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new SamPartyDeleteContext
        {
            PartyId = messagePayload.Identifier,
            BatchId = messagePayload.BatchId
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}
