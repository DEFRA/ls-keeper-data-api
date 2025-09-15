using Amazon.SQS;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Messaging.Consumers;

public class IntakeEventConsumer(
    IAmazonSQS sqsClient,
    IOptions<IntakeEventQueueOptions> options,
    ILogger<IntakeEventConsumer> logger) : QueueConsumerBase<IntakeEventModel>(logger, sqsClient, options)
{
    protected override Task ProcessMessageAsync(IntakeEventModel payload, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}