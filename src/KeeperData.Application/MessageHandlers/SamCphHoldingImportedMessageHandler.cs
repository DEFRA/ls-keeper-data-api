using KeeperData.Application.Orchestration.Sam;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers;

public class SamCphHoldingImportedMessageHandler(SamCphHoldingImportedOrchestrator orchestrator,
  IUnwrappedMessageSerializer<SamCphHoldingImportedMessage> serializer)
  : IMessageHandler<SamCphHoldingImportedMessage>
{
    private readonly IUnwrappedMessageSerializer<SamCphHoldingImportedMessage> _serializer = serializer;
    private readonly SamCphHoldingImportedOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for messageType: SamCphHoldingImportedMessage, messageId: {message.MessageId}, correlationId: {message.CorrelationId}");

        var context = new SamHoldingImportContext
        {
            Cph = messagePayload.Identifier
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}