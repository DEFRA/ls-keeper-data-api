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
    private readonly IAmazonSimpleNotificationService _amazonSimpleNotificationService = amazonSimpleNotificationService;
    private readonly IMessageFactory _messageFactory = messageFactory;
    private readonly IBatchCompletionNotificationConfiguration _configuration = configuration;
    private readonly ILogger<BatchCompletionTopicPublisher> _logger = logger;

    public string? TopicArn => _configuration.BatchCompletionEventsTopic.TopicArn;
    public string? QueueUrl => null;

    public async Task PublishAsync<TMessage>(TMessage? message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            _logger.LogWarning("Attempted to publish null message to batch completion topic");
            return;
        }

        if (string.IsNullOrWhiteSpace(TopicArn))
        {
            _logger.LogWarning("Batch completion topic ARN is not configured, skipping SNS publish");
            return;
        }

        try
        {
            var publishRequest = _messageFactory.CreateSnsMessage(TopicArn, message);
            await _amazonSimpleNotificationService.PublishAsync(publishRequest, cancellationToken);

            _logger.LogInformation("Successfully published batch completion message to SNS topic {TopicArn}", TopicArn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch completion message to SNS topic {TopicArn}", TopicArn);
            // Don't rethrow - we don't want SNS failures to break the completion flow
        }
    }
}