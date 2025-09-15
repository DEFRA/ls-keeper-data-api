using Amazon.SQS;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeeperData.Infrastructure.Messaging.Consumers;

public class IntakeEventConsumer(
    IServiceScopeFactory scopeFactory,
    IAmazonSQS sqsClient,
    IOptions<IntakeEventQueueOptions> options,
    ILogger<IntakeEventConsumer> logger) : QueueConsumerBase<IntakeEventModel>(scopeFactory, sqsClient, options, logger)
{
    protected override Task ProcessMessageAsync(string messageId, IntakeEventModel payload, CancellationToken cancellationToken)
    {
        _observer?.OnMessageHandled(messageId, DateTime.UtcNow, payload);

        return Task.CompletedTask;
    }
}