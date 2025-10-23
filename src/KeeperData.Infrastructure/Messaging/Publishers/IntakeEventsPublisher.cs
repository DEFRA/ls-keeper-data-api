using Amazon.SimpleNotificationService;
using KeeperData.Core.Messaging.Exceptions;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Infrastructure.Messaging.Factories;
using KeeperData.Infrastructure.Messaging.Publishers.Clients;
using KeeperData.Infrastructure.Messaging.Publishers.Configuration;

namespace KeeperData.Infrastructure.Messaging.Publishers;

public class IntakeEventsPublisher(IAmazonSimpleNotificationService amazonSimpleNotificationService,
    IMessageFactory messageFactory, IServiceBusSenderConfiguration serviceBusSenderConfiguration)
    : IMessagePublisher<IntakeEventsTopicClient>
{
    private readonly IAmazonSimpleNotificationService _amazonSimpleNotificationService = amazonSimpleNotificationService;
    private readonly IMessageFactory _messageFactory = messageFactory;
    private readonly IServiceBusSenderConfiguration _serviceBusSenderConfiguration = serviceBusSenderConfiguration;

    public string TopicArn => _serviceBusSenderConfiguration.IntakeEventsTopic.TopicArn;

    public async Task PublishAsync<TMessage>(TMessage? message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentException("Message payload was null", nameof(message));

        if (string.IsNullOrWhiteSpace(TopicArn)) throw new PublishFailedException("TopicArn is missing", false);

        try
        {
            var publishRequest = _messageFactory.CreateMessage(TopicArn, message);
            await _amazonSimpleNotificationService.PublishAsync(publishRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new PublishFailedException($"Failed to publish event on {TopicArn}.", false, ex);
        }
    }
}
