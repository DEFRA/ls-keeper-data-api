using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Services.BatchCompletion;

public class BatchCompletionNotificationService(
    IMessagePublisher<IntakeEventsQueueClient> messagePublisher,
    ILogger<BatchCompletionNotificationService> logger) : IBatchCompletionNotificationService
{
    public async Task NotifyBatchCompletionAsync<TContext>(TContext context, CancellationToken cancellationToken = default)
        where TContext : class
    {
        try
        {
            var completionMessage = CreateCompletionMessage(context);
            if (completionMessage != null)
            {
                await messagePublisher.PublishAsync(completionMessage, cancellationToken);
                logger.LogInformation("Published batch completion message for batch type {BatchType} with correlation ID {CorrelationId}",
                    completionMessage.BatchType, completionMessage.ScanCorrelationId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish batch completion notification for context type {ContextType}", typeof(TContext).Name);
            // Don't rethrow - we don't want completion notifications to fail the actual scan
        }
    }

    private static BatchCompletionMessage? CreateCompletionMessage<TContext>(TContext context) where TContext : class
    {
        return context switch
        {
            SamBulkScanContext samContext => new BatchCompletionMessage
            {
                BatchType = Core.Messaging.Contracts.V1.BatchType.SamBulkScan,
                ScanCorrelationId = samContext.ScanCorrelationId.ToString(),
                BatchStartTime = samContext.CurrentDateTime,
                BatchCompletionTime = DateTime.UtcNow,
                TotalRecordsProcessed = samContext.Holders.TotalCount + samContext.Holdings.TotalCount,
                MessagesPublished = samContext.Holders.CurrentCount + samContext.Holdings.CurrentCount,
                Metadata = new Dictionary<string, object>
                {
                    ["HolderRecordsProcessed"] = samContext.Holders.TotalCount,
                    ["HoldingRecordsProcessed"] = samContext.Holdings.TotalCount,
                    ["HolderMessagesPublished"] = samContext.Holders.CurrentCount,
                    ["HoldingMessagesPublished"] = samContext.Holdings.CurrentCount,
                    ["UpdatedSinceDateTime"] = samContext.UpdatedSinceDateTime?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "null",
                    ["PageSize"] = samContext.PageSize
                }
            },
            CtsBulkScanContext ctsContext => new BatchCompletionMessage
            {
                BatchType = Core.Messaging.Contracts.V1.BatchType.CtsBulkScan,
                ScanCorrelationId = ctsContext.ScanCorrelationId.ToString(),
                BatchStartTime = ctsContext.CurrentDateTime,
                BatchCompletionTime = DateTime.UtcNow,
                TotalRecordsProcessed = ctsContext.Holdings.TotalCount,
                MessagesPublished = ctsContext.Holdings.CurrentCount,
                Metadata = new Dictionary<string, object>
                {
                    ["HoldingRecordsProcessed"] = ctsContext.Holdings.TotalCount,
                    ["HoldingMessagesPublished"] = ctsContext.Holdings.CurrentCount,
                    ["UpdatedSinceDateTime"] = ctsContext.UpdatedSinceDateTime?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "null",
                    ["PageSize"] = ctsContext.PageSize
                }
            },
            _ => null // Don't support other context types yet
        };
    }
}