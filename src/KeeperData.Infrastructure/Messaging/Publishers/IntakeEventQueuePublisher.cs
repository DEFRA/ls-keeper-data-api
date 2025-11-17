using Amazon.SQS;
using KeeperData.Core.Messaging;
using KeeperData.Core.Messaging.Exceptions;
using KeeperData.Core.Messaging.MessagePublishers;
using KeeperData.Core.Messaging.MessagePublishers.Clients;
using KeeperData.Infrastructure.Messaging.Factories;
using KeeperData.Infrastructure.Messaging.Publishers.Configuration;

namespace KeeperData.Infrastructure.Messaging.Publishers;

public class IntakeEventQueuePublisher(IAmazonSQS amazonSQS,
    IMessageFactory messageFactory, IServiceBusSenderConfiguration serviceBusSenderConfiguration)
    : IMessagePublisher<IntakeEventsQueueClient>
{
    private readonly IAmazonSQS _amazonSQS = amazonSQS;
    private readonly IMessageFactory _messageFactory = messageFactory;
    private readonly IServiceBusSenderConfiguration _serviceBusSenderConfiguration = serviceBusSenderConfiguration;

    public string QueueUrl => _serviceBusSenderConfiguration.IntakeEventQueue.QueueUrl;
    public string TopicArn => string.Empty;

    public async Task PublishAsync<TMessage>(TMessage? message, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentException("Message payload was null", nameof(message));

        if (string.IsNullOrWhiteSpace(QueueUrl)) throw new PublishFailedException("QueueUrl is missing", false);

        var correlationId = CorrelationIdContext.Value ?? Guid.NewGuid().ToString();
        var attributes = new Dictionary<string, string>
        {
            ["CorrelationId"] = correlationId
        };

        try
        {
            var sendRequest = _messageFactory.CreateSqsMessage(QueueUrl, message, additionalUserProperties: attributes);
            await _amazonSQS.SendMessageAsync(sendRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new PublishFailedException($"Failed to publish message on {QueueUrl}.", false, ex);
        }
    }
}