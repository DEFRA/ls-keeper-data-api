using KeeperData.Application.Orchestration.Updates.Cts.Holdings;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsUpdateHoldingMessageHandler(
    IUnwrappedMessageSerializer<CtsUpdateHoldingMessage> serializer,
    CtsUpdateHoldingOrchestrator orchestrator)
    : IMessageHandler<CtsUpdateHoldingMessage>
{
    private readonly IUnwrappedMessageSerializer<CtsUpdateHoldingMessage> _serializer = serializer;
    private readonly CtsUpdateHoldingOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(CtsUpdateHoldingMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new CtsUpdateHoldingContext
        {
            Cph = messagePayload.Identifier,
            CurrentDateTime = DateTime.UtcNow
        };

        await _orchestrator.ExecuteAsync(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}