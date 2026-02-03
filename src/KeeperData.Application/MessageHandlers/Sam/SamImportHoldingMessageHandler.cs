using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.Serializers;
using MongoDB.Driver;

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

        ArgumentNullException.ThrowIfNull(message, nameof(message));

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

        string FormatExceptionMessage(string msg) =>
            $"Exception Message: {msg}, Message Identifier: {messagePayload.Identifier}";

        try
        {
            await _orchestrator.ExecuteAsync(context, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new RetryableException(FormatExceptionMessage(ex.Message), ex);
        }
        catch (MongoBulkWriteException ex) when (ex.WriteErrors.Any(e => e.Category == ServerErrorCategory.DuplicateKey))
        {
            throw new RetryableException(FormatExceptionMessage(ex.Message), ex);
        }
        catch (MongoServerException ex) when (ex is MongoBulkWriteException || ex is MongoWriteException)
        {
            throw new NonRetryableException(FormatExceptionMessage(ex.Message), ex);
        }
        catch (Exception ex)
        {
            throw new NonRetryableException(ex.Message, ex);
        }

        return messagePayload;
    }
}