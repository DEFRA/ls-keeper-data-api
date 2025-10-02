using KeeperData.Application.Orchestration.Cts;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers;

public class CtsCphHoldingImportedMessageHandler(CtsCphHoldingImportedOrchestrator orchestrator,
  IUnwrappedMessageSerializer<CtsCphHoldingImportedMessage> serializer)
  : IMessageHandler<CtsCphHoldingImportedMessage>
{
    private readonly IUnwrappedMessageSerializer<CtsCphHoldingImportedMessage> _serializer = serializer;
    private readonly CtsCphHoldingImportedOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for messageType: CtsCphHoldingImportedMessage, messageId: {message.MessageId}, correlationId: {message.CorrelationId}");

        var context = new CtsHoldingImportContext
        {
            Cph = messagePayload.Identifier
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}