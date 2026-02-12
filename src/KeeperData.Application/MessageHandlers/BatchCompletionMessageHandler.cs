using KeeperData.Application.Commands;
using KeeperData.Application.Commands.MessageProcessing;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Core.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace KeeperData.Application.MessageHandlers;

public class BatchCompletionMessageHandler(
    IUnwrappedMessageSerializer<BatchCompletionMessage> serializer,
    IMessagePublisher<BatchCompletionTopicClient> topicPublisher,
    ILogger<BatchCompletionMessageHandler> logger,
    IApplicationMetrics metrics)
    : ICommandHandler<ProcessBatchCompletionMessageCommand, MessageType>
{
    private readonly IApplicationMetrics _metrics = metrics;
    public async Task<MessageType> Handle(ProcessBatchCompletionMessageCommand request, CancellationToken cancellationToken)
    {
        var processingStopwatch = Stopwatch.StartNew();
        var message = request.Message;

        ArgumentNullException.ThrowIfNull(message);

        _metrics.RecordCount(MetricNames.Queue, 1,
            (MetricNames.CommonTags.Operation, MetricNames.Operations.BatchStarted),
            (MetricNames.CommonTags.UpdateType, "batch_completion"));

        logger.LogInformation("Batch completion notification received. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
            message.MessageId, CorrelationIdContext.Value);

        var batchCompletionMessage = serializer.Deserialize(message)
            ?? throw new NonRetryableException($"Deserialisation failed or the message payload was null for " +
            $"messageType: {nameof(BatchCompletionMessage)}," +
            $"messageId: {message.MessageId}," +
            $"correlationId: {message.CorrelationId}");

        logger.LogInformation(
            "Processing batch completion for ScanCorrelationId: {ScanCorrelationId}, ", batchCompletionMessage.ScanCorrelationId);

        // Publish to SNS topic for identity-service
        try
        {
            await topicPublisher.PublishAsync(batchCompletionMessage, cancellationToken);
            logger.LogInformation("Successfully forwarded batch completion to SNS topic for identity-service notifications");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish batch completion to SNS topic, but continuing with processing");
            // Continue processing - SNS failures shouldn't break the handler
        }

        logger.LogInformation("Batch completion notification processed successfully");

        processingStopwatch.Stop();
        
        _metrics.RecordValue(MetricNames.Queue, processingStopwatch.ElapsedMilliseconds,
            (MetricNames.CommonTags.Operation, MetricNames.Operations.BatchDuration));
            
        _metrics.RecordCount(MetricNames.Queue, 1,
            (MetricNames.CommonTags.Operation, MetricNames.Operations.BatchProcessed));

        return await Task.FromResult(batchCompletionMessage);
    }
}