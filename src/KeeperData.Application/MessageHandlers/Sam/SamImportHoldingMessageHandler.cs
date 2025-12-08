using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Sam;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;
using MongoDB.Driver;

namespace KeeperData.Application.MessageHandlers.Sam;

public class SamImportHoldingMessageHandler(SamHoldingImportOrchestrator orchestrator,
  IUnwrappedMessageSerializer<SamImportHoldingMessage> serializer)
  : IMessageHandler<SamImportHoldingMessage>
{
    private readonly IUnwrappedMessageSerializer<SamImportHoldingMessage> _serializer = serializer;
    private readonly SamHoldingImportOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
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

        try
        {
            await _orchestrator.ExecuteAsync(context, cancellationToken);
        }
        catch (MongoBulkWriteException ex)
        {
            throw new RetryableException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            throw new NonRetryableException(ex.Message, ex);
        }

        return await Task.FromResult(messagePayload!);
    }
}