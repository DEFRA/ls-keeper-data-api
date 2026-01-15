using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Application.Orchestration.Updates.Cts.Agents;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1.Cts;
using KeeperData.Core.Messaging.Serializers;
using MongoDB.Driver;

namespace KeeperData.Application.MessageHandlers.Cts;

public class CtsUpdateAgentMessageHandler(
    IUnwrappedMessageSerializer<CtsUpdateAgentMessage> serializer,
    CtsUpdateAgentOrchestrator orchestrator)
    : ICommandHandler<ProcessCtsUpdateAgentMessageCommand, MessageType>
{
    private readonly IUnwrappedMessageSerializer<CtsUpdateAgentMessage> _serializer = serializer;
    private readonly CtsUpdateAgentOrchestrator _orchestrator = orchestrator;

    public async Task<MessageType> Handle(ProcessCtsUpdateAgentMessageCommand request, CancellationToken cancellationToken = default)
    {
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message, nameof(message));

        var messagePayload = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(CtsUpdateAgentMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        var context = new CtsUpdateAgentContext
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
            throw new NonRetryableException($"Exception Message: {ex.Message}, Message Identifier: {messagePayload.Identifier}", ex);
        }
        catch (Exception ex)
        {
            throw new NonRetryableException(ex.Message, ex);
        }

        return await Task.FromResult(messagePayload!);
    }
}