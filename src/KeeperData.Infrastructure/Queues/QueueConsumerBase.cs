using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Queues;

public abstract class QueueConsumerBase<T>(
    ILogger<QueueConsumerBase<T>> logger,
    IAmazonSQS sqsClient,
    IOptions<QueueConsumerOptions> options)
    : IHostedService, IDisposable
{
    private readonly QueueConsumerOptions _queueConsumerOptions = options.Value;

    public void Dispose()
    {
        sqsClient.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("QueueConsumerBase Service started.");

        if (_queueConsumerOptions?.Disabled == true)
        {
            logger.LogInformation($"Queue [{_queueConsumerOptions.QueueUrl}] disabled in config");
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
            logger.LogInformation($"Connecting to queue: {_queueConsumerOptions.QueueUrl}");
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogTrace($"Entering query loop for: {_queueConsumerOptions.QueueUrl}");
                try
                {
                    var sqsResponse = await sqsClient.ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = _queueConsumerOptions.QueueUrl,
                            MaxNumberOfMessages = _queueConsumerOptions.MaxNumberOfMessages,
                            WaitTimeSeconds = _queueConsumerOptions.WaitTimeSeconds
                        }, cancellationToken);

                    logger.LogTrace($"Completed receive for: {_queueConsumerOptions.QueueUrl} Number of messages: {sqsResponse.Messages.Count}");

                    sqsResponse.Messages.ForEach(x =>
                    {
                        var payload = JsonSerializer.Deserialize<T>(x.Body);

                        if (payload == null) throw new ArgumentOutOfRangeException(nameof(payload));

                        ProcessMessageAsync(payload, cancellationToken);
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError($"Unable to connect to queue: {_queueConsumerOptions.QueueUrl} - Exception: {ex}");
                    // Wait for queue creation, refactor with Polly later
                    Thread.Sleep(1000);
                }
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }
}

public record QueueConsumerOptions
{
    public required string QueueUrl { get; init; }
    public int MaxNumberOfMessages { get; init; }
    public int WaitTimeSeconds { get; init; }
    public bool Disabled { get; init; } = false;
}