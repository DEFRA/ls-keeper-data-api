using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.Updates.Cts.Holdings;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Serializers;
using MediatR;
using MongoDB.Driver;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsUpdateHoldingMessageHandler(
    IUnwrappedMessageSerializer<CtsUpdateHoldingMessage> serializer,
    CtsUpdateHoldingOrchestrator orchestrator)
    : IRequestHandler<ProcessCtsUpdateHoldingMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<CtsUpdateHoldingMessage> _serializer = serializer;
    private readonly CtsUpdateHoldingOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(ProcessCtsUpdateHoldingMessageCommand request, CancellationToken cancellationToken = default)
    {
        var message = request.Message;

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

        try
        {
            await _orchestrator.ExecuteAsync(context, cancellationToken);
        }
        catch (MongoBulkWriteException ex)
        {
            throw new NonRetryableException($"Exception Message: {ex.Message}, Message Identifier: {messagePayload.Identifier}", ex);
        }
        catch (Exception ex)
        {
            throw new NonRetryableException(ex.Message, ex);
        }

        return await Task.FromResult(messagePayload!);
    }
}