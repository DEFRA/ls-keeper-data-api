using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.Extentions;
using KeeperData.Application.Orchestration.Imports.Cts.Holdings;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Serializers;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsImportHoldingMessageHandler(CtsHoldingImportOrchestrator orchestrator,
  IUnwrappedMessageSerializer<CtsImportHoldingMessage> serializer)
  : ICommandHandler<ProcessCtsImportHoldingMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<CtsImportHoldingMessage> _serializer = serializer;
    private readonly CtsHoldingImportOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(ProcessCtsImportHoldingMessageCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message);

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(CtsImportHoldingMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new CtsHoldingImportContext
        {
            Cph = messagePayload.Identifier,
            BatchId = messagePayload.BatchId,
            CurrentDateTime = DateTime.UtcNow
        };

        await _orchestrator.TryExecuteAndThrowRetryable(context, cancellationToken);

        return await Task.FromResult(messagePayload!);
    }
}