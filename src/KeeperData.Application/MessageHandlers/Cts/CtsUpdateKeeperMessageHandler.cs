using KeeperData.Application.Orchestration.Updates.Cts.Keepers;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;
using MongoDB.Driver;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsUpdateKeeperMessageHandler(
    IUnwrappedMessageSerializer<CtsUpdateKeeperMessage> serializer,
    CtsUpdateKeeperOrchestrator orchestrator)
    : IMessageHandler<CtsUpdateKeeperMessage>
{
    private readonly IUnwrappedMessageSerializer<CtsUpdateKeeperMessage> _serializer = serializer;
    private readonly CtsUpdateKeeperOrchestrator _orchestrator = orchestrator;
    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

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