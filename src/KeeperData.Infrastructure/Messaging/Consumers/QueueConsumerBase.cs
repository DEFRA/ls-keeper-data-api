using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Infrastructure.Messaging.Configuration;
using KeeperData.Infrastructure.Messaging.Observers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KeeperData.Infrastructure.Messaging.Consumers;

public abstract class QueueConsumerBase<T> : IHostedService, IDisposable
{
    protected readonly IServiceScopeFactory _scopeFactory;
    protected readonly IAmazonSQS _sqsClient;
    protected readonly QueueConsumerOptions _queueConsumerOptions;
    protected readonly IQueueConsumerObserver<T>? _observer;
    protected readonly ILogger<QueueConsumerBase<T>> _logger;

    protected QueueConsumerBase(
        IServiceScopeFactory scopeFactory,
        IAmazonSQS sqsClient,
        IOptions<QueueConsumerOptions> options,
        ILogger<QueueConsumerBase<T>> logger)
    {
        _scopeFactory = scopeFactory;
        _sqsClient = sqsClient;
        _queueConsumerOptions = options.Value;
        _logger = logger;

        using var scope = _scopeFactory.CreateScope();
        _observer = scope.ServiceProvider.GetService<IQueueConsumerObserver<T>>();
    }


    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sqsClient?.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueConsumerBase Service started.");

        if (_queueConsumerOptions?.Disabled == true)
        {
            _logger.LogInformation("Queue {queueUrl} disabled in config", _queueConsumerOptions.QueueUrl);

            return Task.CompletedTask;
        }

        return ExecuteQueryLoop(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueueConsumerBase Service stopping.");

        return Task.CompletedTask;
    }

    protected abstract Task ProcessMessageAsync(string messageId, T payload, CancellationToken cancellationToken);

    private Task ExecuteQueryLoop(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            _logger.LogInformation("Connecting to queue: {queueUrl}", _queueConsumerOptions.QueueUrl);

            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogTrace("Entering query loop for: {queueUrl}", _queueConsumerOptions.QueueUrl);

                try
                {
                    var sqsResponse = await _sqsClient.ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = _queueConsumerOptions.QueueUrl,
                            MaxNumberOfMessages = _queueConsumerOptions.MaxNumberOfMessages,
                            WaitTimeSeconds = _queueConsumerOptions.WaitTimeSeconds
                        }, cancellationToken);

                    _logger.LogTrace("Completed receive for: {queueUrl} Number of messages: {count}",
                        _queueConsumerOptions.QueueUrl, sqsResponse.Messages.Count);

                    sqsResponse.Messages.ForEach(x =>
                    {
                        var payload = JsonSerializer.Deserialize<T>(x.Body)
                            ?? throw new Exception("Message payload is missing.");

                        ProcessMessageAsync(x.MessageId, payload, cancellationToken);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError("Unable to connect to queue: {queueUrl} - Exception: {ex}",
                        _queueConsumerOptions.QueueUrl, ex);

                    // Wait for queue creation, refactor with Polly later
                    Thread.Sleep(1000);
                }
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }
}