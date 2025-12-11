namespace KeeperData.Core.Messaging.MessagePublishers;

public interface IMessagePublisher<in T> where T : class, new()
{
    string? TopicArn { get; }

    string? QueueUrl { get; }

    Task PublishAsync<TMessage>(TMessage? message, CancellationToken cancellationToken = default);
}