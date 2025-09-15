using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KeeperData.Infrastructure.Messaging.Consumers;

public abstract class QueueConsumerBase<T>(
    ILogger<QueueConsumerBase<T>> logger,
    IAmazonSQS sqsClient,
    IOptions<QueueConsumerOptions> options)
    : IHostedService, IDisposable
{
    private readonly QueueConsumerOptions _queueConsumerOptions = options.Value;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            sqsClient?.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("QueueConsumerBase Service started.");

        if (_queueConsumerOptions?.Disabled == true)
        {
            logger.LogInformation("Queue {queueUrl} disabled in config", _queueConsumerOptions.QueueUrl);

            return Task.CompletedTask;
        }

        return ExecuteQueryLoop(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("QueueConsumerBase Service stopping.");

        return Task.CompletedTask;
    }

    protected abstract Task ProcessMessageAsync(T payload, CancellationToken cancellationToken);

    private Task ExecuteQueryLoop(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            logger.LogInformation("Connecting to queue: {queueUrl}", _queueConsumerOptions.QueueUrl);
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogTrace("Entering query loop for: {queueUrl}", _queueConsumerOptions.QueueUrl);
                try
                {
                    var sqsResponse = await sqsClient.ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = _queueConsumerOptions.QueueUrl,
                            MaxNumberOfMessages = _queueConsumerOptions.MaxNumberOfMessages,
                            WaitTimeSeconds = _queueConsumerOptions.WaitTimeSeconds
                        }, cancellationToken);

                    logger.LogTrace("Completed receive for: {queueUrl} Number of messages: {count}",
                        _queueConsumerOptions.QueueUrl, sqsResponse.Messages.Count);

                    sqsResponse.Messages.ForEach(x =>
                    {
                        var payload = JsonSerializer.Deserialize<T>(x.Body) ?? throw new Exception("Message payload is missing.");
                        ProcessMessageAsync(payload, cancellationToken);
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError("Unable to connect to queue: {queueUrl} - Exception: {ex}",
                        _queueConsumerOptions.QueueUrl, ex);

                    // Wait for queue creation, refactor with Polly later
                    Thread.Sleep(1000);
                }
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }
}