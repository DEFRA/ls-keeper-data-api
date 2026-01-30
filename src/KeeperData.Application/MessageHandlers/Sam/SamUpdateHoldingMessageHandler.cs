using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.Serializers;
using MongoDB.Driver;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamUpdateHoldingMessageHandler(
    SamHoldingImportOrchestrator orchestrator,
    IUnwrappedMessageSerializer<SamUpdateHoldingMessage> serializer)
    : ICommandHandler<ProcessSamUpdateHoldingMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<SamUpdateHoldingMessage> _serializer = serializer;
    private readonly SamHoldingImportOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(ProcessSamUpdateHoldingMessageCommand request, CancellationToken cancellationToken)
    {
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(SamUpdateHoldingMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new SamHoldingImportContext
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