using KeeperData.Core.Messaging;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.MessageHandlers;

public class BatchCompletionMessageHandler(
    IUnwrappedMessageSerializer<BatchCompletionMessage> serializer,
    ILogger<BatchCompletionMessageHandler> logger)
    : IMessageHandler<BatchCompletionMessage>
{
    private readonly IUnwrappedMessageSerializer<BatchCompletionMessage> _serializer = serializer;
    private readonly ILogger<BatchCompletionMessageHandler> _logger = logger;

    public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));

        _logger.LogInformation("Batch completion notification received. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
            message.MessageId, CorrelationIdContext.Value);

        var batchCompletionMessage = _serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {typeof(BatchCompletionMessage).Name}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        _logger.LogInformation(
            "Processing batch completion for {BatchType}. " +
            "ScanCorrelationId: {ScanCorrelationId}, " +
            "TotalRecords: {TotalRecords}, " +
            "MessagesPublished: {MessagesPublished}, " +
            "Duration: {Duration}ms",
            batchCompletionMessage.BatchType,
            batchCompletionMessage.ScanCorrelationId,
            batchCompletionMessage.TotalRecordsProcessed,
            batchCompletionMessage.MessagesPublished,
            (batchCompletionMessage.BatchCompletionTime - batchCompletionMessage.BatchStartTime).TotalMilliseconds);

        // TODO: Implement notification logic here

        _logger.LogInformation("Batch completion notification processed successfully. BatchType: {BatchType}",
            batchCompletionMessage.BatchType);

        return await Task.FromResult(batchCompletionMessage);
    }
}