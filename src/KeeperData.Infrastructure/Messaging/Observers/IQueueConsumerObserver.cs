namespace KeeperData.Infrastructure.Messaging.Observers;

public interface IQueueConsumerObserver<T>
{
    void OnMessageHandled(string messageId, DateTime handledAt, T payload);
    void OnMessageFailed(string messageId, DateTime failedAt, Exception exception);
}