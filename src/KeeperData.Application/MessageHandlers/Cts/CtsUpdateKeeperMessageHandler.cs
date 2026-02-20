using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.Extentions;
using KeeperData.Application.Orchestration.Updates.Cts.Keepers;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsUpdateKeeperMessageHandler(
    IUnwrappedMessageSerializer<CtsUpdateKeeperMessage> serializer,
    CtsUpdateKeeperOrchestrator orchestrator)
    : ICommandHandler<ProcessCtsUpdateKeeperMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<CtsUpdateKeeperMessage> _serializer = serializer;
    private readonly CtsUpdateKeeperOrchestrator _orchestrator = orchestrator;
    public async Task<MessageType> Handle(ProcessCtsUpdateKeeperMessageCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message);

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(CtsUpdateKeeperMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new CtsUpdateKeeperContext
        {
            PartyId = messagePayload.Identifier,
            CurrentDateTime = DateTime.UtcNow
        };

        await _orchestrator.TryExecuteAndThrowRetryable(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}