using KeeperData.Infrastructure.Messaging.Observers;

namespace KeeperData.Api.Tests.Integration.Consumers.Helpers;

public class TestConsumerObserver<T> : IQueueConsumerObserver<T>
{
    private readonly TaskCompletionSource<(string MessageId, T Payload)> _handledTcs = new();
    private readonly TaskCompletionSource<(string MessageId, Exception Error)> _failedTcs = new();

    public Task<(string MessageId, T Payload)> MessageHandled => _handledTcs.Task;
    public Task<(string MessageId, Exception Error)> MessageFailed => _failedTcs.Task;

    public void OnMessageHandled(string messageId, DateTime handledAt, T payload)
    {
        _handledTcs.TrySetResult((messageId, payload));
    }

    public void OnMessageFailed(string messageId, DateTime failedAt, Exception exception)
    {
        _failedTcs.TrySetResult((messageId, exception));
    }
}