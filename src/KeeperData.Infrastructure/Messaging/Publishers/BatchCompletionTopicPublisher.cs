using Amazon.SimpleNotificationService;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Factories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Infrastructure.Messaging.Publishers;

public class BatchCompletionTopicPublisher(
    IAmazonSimpleNotificationService amazonSimpleNotificationService,
    IMessageFactory messageFactory,
    IBatchCompletionNotificationConfiguration configuration,
    ILogger<BatchCompletionTopicPublisher> logger) : IMessagePublisher<BatchCompletionTopicClient>
{
    public string? TopicArn => configuration.BatchCompletionEventsTopic.TopicArn;
    public string? QueueUrl => null;

    public async Task PublishAsync<TMessage>(TMessage? message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            logger.LogWarning("Attempted to publish null message to batch completion topic");
            return;
        }

        if (string.IsNullOrWhiteSpace(TopicArn))
        {
            logger.LogWarning("Batch completion topic ARN is not configured, skipping SNS publish");
            return;
        }

        try
        {
            var publishRequest = messageFactory.CreateSnsMessage(TopicArn, message);
            await amazonSimpleNotificationService.PublishAsync(publishRequest, cancellationToken);

            logger.LogInformation("Successfully published batch completion message to SNS topic {TopicArn}", TopicArn);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish batch completion message to SNS topic {TopicArn}", TopicArn);
            // Don't rethrow - we don't want SNS failures to break the completion flow
        }
    }
}