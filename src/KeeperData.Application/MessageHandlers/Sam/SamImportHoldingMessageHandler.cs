using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.Extentions;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamImportHoldingMessageHandler(SamHoldingImportOrchestrator orchestrator,
  IUnwrappedMessageSerializer<SamImportHoldingMessage> serializer)
  : ICommandHandler<ProcessSamImportHoldingMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<SamImportHoldingMessage> _serializer = serializer;
    private readonly SamHoldingImportOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(ProcessSamImportHoldingMessageCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message);

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamImportHoldingMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new SamHoldingImportContext
        {
            Cph = messagePayload.Identifier,
            CurrentDateTime = DateTime.UtcNow
        };

        await _orchestrator.TryExecuteAndThrowRetryable(context, cancellationToken);

        return messagePayload;
    }
}