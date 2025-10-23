using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;

namespace KeeperData.Infrastructure.Messaging.Factories;

public interface IMessageFactory
{
    PublishRequest CreateSnsMessage<TBody>(
        string topicArn,
        TBody body,
        string? subject = null,
        Dictionary<string, string>? additionalUserProperties = null);

    SendMessageRequest CreateSqsMessage<TBody>(
        string queueUrl,
        TBody body,
        string? subject = null,
        Dictionary<string, string>? additionalUserProperties = null);
}