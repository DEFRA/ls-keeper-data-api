using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Core.Exceptions;
using KeeperData.Core.Messaging;
using KeeperData.Core.Messaging.Consumers;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.Extensions;
using KeeperData.Core.Messaging.Observers;
using KeeperData.Core.Messaging.Serializers;
using KeeperData.Core.Messaging.Throttling;
using KeeperData.Core.Providers;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Factories.Implementations;
using KeeperData.Infrastructure.Messaging.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Messaging.Consumers;

public class QueuePoller(IServiceScopeFactory scopeFactory,
    IAmazonSQS amazonSQS,
    IMessageSerializer<SnsEnvelope> messageSerializer,
    IDeadLetterQueueService deadLetterQueueService,
    MessageCommandRegistry messageCommandRegistry,
    IDataImportThrottlingConfiguration dataImportThrottlingConfiguration,
    IOptions<IntakeEventQueueOptions> options,
    IDelayProvider delayProvider,
    IQueuePollerObserver<MessageType> observer,
    ILogger<QueuePoller> logger) : IQueuePoller, IAsyncDisposable
{
    private readonly IntakeEventQueueOptions _queueConsumerOptions = options.Value;

    private Task? _pollingTask;
    private CancellationTokenSource _cts = new();

    public Task StartAsync(CancellationToken token)
    {
        logger.LogInformation("QueuePoller start requested.");

        if (_queueConsumerOptions.Disabled == true)
        {
            logger.LogInformation("Queue {queueUrl} disabled in config", _queueConsumerOptions.QueueUrl);

            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        _pollingTask = Task.Run(() => PollMessagesAsync(_cts.Token), token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken token)
    {
        logger.LogInformation("QueuePoller stop requested.");

        await _cts.CancelAsync();

        if (_pollingTask is { IsCompletedSuccessfully: false })
        {
            try
            {
                await _pollingTask;
            }
            catch (TaskCanceledException)
            {
                // Expected during cancellation
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
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
        logger.LogInformation("Connecting to queue: {queueUrl}", _queueConsumerOptions.QueueUrl);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await amazonSQS.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueConsumerOptions.QueueUrl,
                    MaxNumberOfMessages = _queueConsumerOptions.MaxNumberOfMessages,
                    WaitTimeSeconds = _queueConsumerOptions.WaitTimeSeconds,
                    MessageAttributeNames = ["All"],
                    MessageSystemAttributeNames = ["All"]
                }, cancellationToken);

                var messages = response?.Messages;

                if (messages == null || messages.Count == 0) continue;

                logger.LogTrace("Completed receive for queue: {queueUrl}, Number of messages: {count}",
                    _queueConsumerOptions.QueueUrl, messages.Count);

                foreach (var message in messages)
                {
                    await HandleMessageAsync(message, _queueConsumerOptions.QueueUrl, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Poll operation cancelled for queue {queueUrl}", _queueConsumerOptions.QueueUrl);
            }
            catch (Exception ex)
            {
                logger.LogError("Unable to connect to queue: {queueUrl} - Exception: {ex}",
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
            var unwrappedMessage = message.Unwrap(messageSerializer);
            CorrelationIdContext.Value = string.IsNullOrWhiteSpace(unwrappedMessage.CorrelationId)
                ? Guid.NewGuid().ToString()
                : unwrappedMessage.CorrelationId;

            logger.LogDebug("HandleMessageAsync using correlationId: {correlationId}", CorrelationIdContext.Value);

            var command = messageCommandRegistry.CreateCommand(unwrappedMessage);

            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(command, cancellationToken);

            if (dataImportThrottlingConfiguration.MessageCompletionDelayMs > 0)
            {
                await ThrottleMessageProcessing(dataImportThrottlingConfiguration.MessageCompletionDelayMs, cancellationToken);
            }

            await amazonSQS.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);

            logger.LogInformation("Handled message with correlationId: {correlationId}", CorrelationIdContext.Value);

            observer?.OnMessageHandled(message.MessageId, DateTime.UtcNow, result, message);
        }
        catch (RetryableException ex)
        {
            await HandleRetryableException(message, queueUrl, ex, cancellationToken);
        }
        catch (NonRetryableException ex)
        {
            await HandleNonRetryableException(message, queueUrl, ex, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleUnexpectedException(message, queueUrl, ex, cancellationToken);
        }
    }

    private async Task HandleRetryableException(Message message, string queueUrl, RetryableException ex, CancellationToken cancellationToken)
    {
        var receiveCount = GetReceiveCount(message);
        var maxRetries = _queueConsumerOptions.MaxReceiveCount;

        if (receiveCount >= maxRetries)
        {
            logger.LogError("RetryableException exceeded max retries ({maxRetries}) in queue: {queue}, correlationId: {correlationId}, messageId: {messageId}, Exception: {ex}",
                maxRetries, _queueConsumerOptions.QueueUrl, CorrelationIdContext.Value, message.MessageId, ex);

            await MoveToDlqAndNotifyObserver(message, queueUrl, ex, cancellationToken);
        }
        else
        {
            logger.LogWarning("RetryableException in queue: {queue}, correlationId: {correlationId}, messageId: {messageId}, receiveCount: {receiveCount}/{maxRetries}, Exception: {ex}",
                _queueConsumerOptions.QueueUrl, CorrelationIdContext.Value, message.MessageId, receiveCount, maxRetries, ex);

            observer?.OnMessageFailed(message.MessageId, DateTime.UtcNow, ex, message);
        }
    }

    private async Task HandleNonRetryableException(Message message, string queueUrl, NonRetryableException ex, CancellationToken cancellationToken)
    {
        logger.LogError("NonRetryableException in queue: {queue}, correlationId: {correlationId}, messageId: {messageId}, Exception: {ex}",
            _queueConsumerOptions.QueueUrl, CorrelationIdContext.Value, message.MessageId, ex);

        await MoveToDlqAndNotifyObserver(message, queueUrl, ex, cancellationToken);
    }

    private async Task HandleUnexpectedException(Message message, string queueUrl, Exception ex, CancellationToken cancellationToken)
    {
        logger.LogError("Unhandled Exception in queue: {queue}, correlationId: {correlationId}, messageId: {messageId}, Exception: {ex}",
            _queueConsumerOptions.QueueUrl, CorrelationIdContext.Value, message.MessageId, ex);

        await MoveToDlqAndNotifyObserver(message, queueUrl, ex, cancellationToken);
    }

    private async Task MoveToDlqAndNotifyObserver(Message message, string queueUrl, Exception ex, CancellationToken cancellationToken)
    {
        await deadLetterQueueService.MoveToDeadLetterQueueAsync(message, queueUrl, ex, cancellationToken);
        observer?.OnMessageFailed(message.MessageId, DateTime.UtcNow, ex, message);
    }

    private static int GetReceiveCount(Message message)
    {
        if (message.Attributes?.TryGetValue("ApproximateReceiveCount", out var countStr) == true
            && int.TryParse(countStr, out var count))
        {
            return count;
        }
        return 0;
    }

    private async Task ThrottleMessageProcessing(int messageCompletionDelayMs, CancellationToken cancellationToken)
    {
        logger.LogDebug("HandleMessageAsync throttling message completion: waiting {messageCompletionDelayMs} ms before completing",
            messageCompletionDelayMs);

        await delayProvider.DelayAsync(
            TimeSpan.FromMilliseconds(messageCompletionDelayMs),
            cancellationToken);
    }
}