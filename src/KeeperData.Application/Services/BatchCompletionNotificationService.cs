using KeeperData.Core.Messaging.Contracts.V1;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Core.Orchestration;
using KeeperData.Core.Services;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Services.BatchCompletion;

public class BatchCompletionNotificationService(
    IMessagePublisher<IntakeEventsQueueClient> messagePublisher,
    ILogger<BatchCompletionNotificationService> logger) : IBatchCompletionNotificationService
{
    public async Task NotifyBatchCompletionAsync<TContext>(TContext context, CancellationToken cancellationToken = default)
        where TContext : IScanContext
    {
        try
        {
            var completionMessage = new BatchCompletionMessage
            {
                ScanCorrelationId = context.ScanCorrelationId.ToString()
            };

            await messagePublisher.PublishAsync(completionMessage, cancellationToken);
            logger.LogInformation("Published batch completion message for batch type {ContextType} correlation ID {CorrelationId}",
                typeof(TContext).Name, completionMessage.ScanCorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish batch completion notification for context type {ContextType}", typeof(TContext).Name);
            // Don't rethrow - we don't want completion notifications to fail the actual scan
        }
    }
}