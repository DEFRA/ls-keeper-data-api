using KeeperData.Core.Messaging;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.MessageHandlers;

public class BatchCompletionMessageHandler(
    IUnwrappedMessageSerializer<BatchCompletionMessage> serializer,
    IMessagePublisher<BatchCompletionTopicClient> topicPublisher,
    ILogger<BatchCompletionMessageHandler> logger)
    : IMessageHandler<BatchCompletionMessage>
{
    private readonly IUnwrappedMessageSerializer<BatchCompletionMessage> _serializer = serializer;
    private readonly IMessagePublisher<BatchCompletionTopicClient> _topicPublisher = topicPublisher;
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

        // Publish to SNS topic for identity-service
        try
        {
            await _topicPublisher.PublishAsync(batchCompletionMessage, cancellationToken);
            _logger.LogInformation("Successfully forwarded batch completion to SNS topic for identity-service notifications. BatchType: {BatchType}",
                batchCompletionMessage.BatchType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch completion to SNS topic, but continuing with processing. BatchType: {BatchType}",
                batchCompletionMessage.BatchType);
            // Continue processing - SNS failures shouldn't break the handler
        }

        _logger.LogInformation("Batch completion notification processed successfully. BatchType: {BatchType}",
            batchCompletionMessage.BatchType);

        return await Task.FromResult(batchCompletionMessage);
    }
}