using Amazon.SQS;
using Amazon.SQS.Model;
using KeeperData.Infrastructure.Queues;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KeeperData.Core.Health;

public class QueueHealthCheck<T>(T queueOptions, IAmazonSQS sqsClient) : IHealthCheck
    where T : QueueConsumerOptions
{
    string _queueUrl = queueOptions.QueueUrl;
    TimeSpan _timeout = TimeSpan.FromSeconds(queueOptions.WaitTimeSeconds);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        Exception? exception = null;
        GetQueueAttributesResponse? attributes = null;
        try
        {
            attributes = await sqsClient.GetQueueAttributesAsync(_queueUrl, ["All"], cancellationToken);
        }
        catch (TaskCanceledException)
        {
            exception = new TimeoutException($"The queue check was cancelled, probably because it timed out after {queueOptions.WaitTimeSeconds} seconds");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        var healthStatus = attributes != null ? HealthStatus.Healthy : HealthStatus.Degraded;

        var data = attributes == null ? [] : new Dictionary<string, object>
        {
            { "queue-url", _queueUrl },
            { "approximate-number-of-messages", attributes.ApproximateNumberOfMessages },
            { "approximate-number-of-messages-delayed", attributes.ApproximateNumberOfMessagesDelayed },
            { "approximate-number-of-messages-not-visible", attributes.ApproximateNumberOfMessagesNotVisible },
            { "content-length", attributes.ContentLength }
        };

        if (exception != null)
        {
            healthStatus = HealthStatus.Unhealthy;
            data.Add("error", $"{exception.Message} - {exception.InnerException?.Message}");
        }

        return new HealthCheckResult(
            status: healthStatus,
            description: $"Health check on queue: {_queueUrl}",
            exception: exception,
            data: data);
    }
}