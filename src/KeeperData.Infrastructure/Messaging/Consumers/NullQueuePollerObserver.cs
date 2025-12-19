using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Observers;

namespace KeeperData.Infrastructure.Messaging.Consumers;

public class NullQueuePollerObserver<T> : IQueuePollerObserver<T>
{
    public void OnMessageHandled(string messageId, DateTime handledAt, T payload, Message rawMessage) { }
    public void OnMessageFailed(string messageId, DateTime failedAt, Exception exception, Message rawMessage) { }
}
