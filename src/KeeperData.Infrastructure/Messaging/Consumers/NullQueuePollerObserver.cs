using Amazon.SQS.Model;
using KeeperData.Core.Messaging.Observers;
using System.Diagnostics.CodeAnalysis;

namespace KeeperData.Infrastructure.Messaging.Consumers;

[ExcludeFromCodeCoverage]
public class NullQueuePollerObserver<T> : IQueuePollerObserver<T>
{
    public void OnMessageHandled(string messageId, DateTime handledAt, T payload, Message rawMessage) { }
    public void OnMessageFailed(string messageId, DateTime failedAt, Exception exception, Message rawMessage) { }
}