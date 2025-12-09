using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Cts.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Services;
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
                logger.LogInformation("Published batch completion message for batch type correlation ID {CorrelationId}",
                    completionMessage.ScanCorrelationId);
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
                ScanCorrelationId = samContext.ScanCorrelationId.ToString(),
            },
            CtsBulkScanContext ctsContext => new BatchCompletionMessage
            {
                ScanCorrelationId = ctsContext.ScanCorrelationId.ToString()
            },
            _ => null // Don't support other context types yet
        };
    }
}