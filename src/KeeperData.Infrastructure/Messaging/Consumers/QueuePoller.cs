using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Extensions;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Core.Messaging.Observers;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Messaging.Consumers;

public class QueuePoller(IServiceScopeFactory scopeFactory,
    IAmazonSQS amazonSQS,
    IMessageHandlerManager messageHandlerManager,
    IMessageSerializer<SnsEnvelope> messageSerializer,
    IOptions<IntakeEventQueueOptions> options,
    ILogger<QueuePoller> logger) : IQueuePoller, IAsyncDisposable
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IAmazonSQS _amazonSQS = amazonSQS;
    private readonly IMessageHandlerManager _messageHandlerManager = messageHandlerManager;
    private readonly IMessageSerializer<SnsEnvelope> _messageSerializer = messageSerializer;
    private readonly IntakeEventQueueOptions _queueConsumerOptions = options.Value;
    private readonly ILogger<QueuePoller> _logger = logger;

    private IQueuePollerObserver<MessageType>? _observer;

    private Task? _pollingTask;
    private CancellationTokenSource? _cts;

    private const string MESSAGE_SUFFIX = "Message";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueuePoller start requested.");

        using var scope = _scopeFactory.CreateScope();
        _observer = scope.ServiceProvider.GetService<IQueuePollerObserver<MessageType>>();

        if (_queueConsumerOptions?.Disabled == true)
        {
            _logger.LogInformation("Queue {queueUrl} disabled in config", _queueConsumerOptions.QueueUrl);

            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _pollingTask = Task.Run(() => PollMessagesAsync(_cts.Token), cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueuePoller stop requested.");

        _cts?.Cancel();

        if (_pollingTask is { IsCompletedSuccessfully: false })
        {
            await _pollingTask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        if (_pollingTask is { IsCompleted: false })
        {
            try
            {
                await _pollingTask;
            }
            catch (TaskCanceledException)
            {
                // Swallow expected task cancellation during disposal
            }
        }

        GC.SuppressFinalize(this);
    }

    private async Task PollMessagesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting to queue: {queueUrl}", _queueConsumerOptions.QueueUrl);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _amazonSQS.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueConsumerOptions.QueueUrl,
                    MaxNumberOfMessages = _queueConsumerOptions.MaxNumberOfMessages,
                    WaitTimeSeconds = _queueConsumerOptions.WaitTimeSeconds,
                    MessageAttributeNames = ["All"]
                }, cancellationToken);

                if (response?.Messages.Count == 0) continue;

                _logger.LogTrace("Completed receive for queue: {queueUrl}, Number of messages: {count}",
                    _queueConsumerOptions.QueueUrl, response?.Messages.Count);

                if (response?.Messages?.Count > 0)
                {
                    foreach (var message in response.Messages)
                    {
                        await HandleMessageAsync(message, _queueConsumerOptions.QueueUrl, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to connect to queue: {queueUrl} - Exception: {ex}",
                    _queueConsumerOptions.QueueUrl, ex);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private async Task HandleMessageAsync(Message message, string queueUrl, CancellationToken cancellationToken)
    {
        try
        {
            var unwrappedMessage = message.Unwrap(_messageSerializer);

            var handlerTypes = _messageHandlerManager.GetHandlersForMessage(unwrappedMessage.Subject);

            foreach (var handlerInfo in handlerTypes)
            {
                var messageType = _messageHandlerManager.GetMessageTypeByName($"{unwrappedMessage.Subject}{MESSAGE_SUFFIX}");

                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetService(handlerInfo.HandlerType);
                if (handler == null) continue;

                var concreteType = typeof(IMessageHandler<>).MakeGenericType(messageType);
                var messagePayload = await (Task<MessageType>)concreteType.GetMethod("Handle")!.Invoke(handler, [unwrappedMessage, cancellationToken])!;

                await _amazonSQS.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);

                _logger.LogInformation("Handled message with CorrelationId: {correlationId}", unwrappedMessage.CorrelationId);

                _observer?.OnMessageHandled(message.MessageId, DateTime.UtcNow, messagePayload, message);
            }
        }
        catch (RetryableException ex)
        {
            // SQS doesn't support abandon so let visibility timeout expire.

            _logger.LogError("RetryableException in queue: {queue}, messageId: {messageId}, Exception: {ex}",
                _queueConsumerOptions.QueueUrl, message.MessageId, ex);

            _observer?.OnMessageFailed(message.MessageId, DateTime.UtcNow, ex, message);
        }
        catch (NonRetryableException ex)
        {
            // "Move to a DLQ by configuration" - TODO

            _logger.LogError("NonRetryableException in queue: {queue}, messageId: {messageId}, Exception: {ex}",
                _queueConsumerOptions.QueueUrl, message.MessageId, ex);

            _observer?.OnMessageFailed(message.MessageId, DateTime.UtcNow, ex, message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unhandled Exception in queue: {queue}, messageId: {messageId}, Exception: {ex}",
                _queueConsumerOptions.QueueUrl, message.MessageId, ex);

            _observer?.OnMessageFailed(message.MessageId, DateTime.UtcNow, ex, message);
        }
    }
}